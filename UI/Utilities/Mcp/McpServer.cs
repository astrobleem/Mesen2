using Mesen.Config;
using Mesen.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace Mesen.Utilities.Mcp;

/// <summary>
/// Newline-delimited JSON-RPC 2.0 server implementing a minimal subset of
/// the Model Context Protocol. Listens on a TCP port rather than stdio —
/// Mesen.exe is a WinExe (GUI subsystem) and its stdio handling is flaky
/// when launched with redirected pipes. TCP is portable, simple to test
/// from Python/Node, and leaves stdout free for debugger logs.
///
/// One message per line on the socket; responses one per line back. No
/// Content-Length framing.
///
/// Started by McpRunner after the ROM is loaded and the emulator thread
/// is running at max speed. Accepts one connection at a time. Returns
/// when the client disconnects (or the server is told to shut down).
/// </summary>
internal sealed class McpServer
{
	private readonly int _port;
	private readonly Dictionary<string, Func<JsonNode?, JsonNode>> _tools;
	private TextReader? _in;
	private TextWriter? _out;
	private bool _shuttingDown;

	public McpServer(int port)
	{
		_port = port;
		_tools = BuildToolTable();
	}

	public void Run()
	{
		// Bind to loopback only so we don't open a remote attack surface;
		// anyone with local access to the machine can connect.
		TcpListener listener = new(IPAddress.Loopback, _port);
		try {
			listener.Start();
		} catch(Exception ex) {
			WriteLog($"[mcp] bind to port {_port} failed: {ex.Message}");
			return;
		}
		WriteLog($"[mcp] listening on 127.0.0.1:{_port}; {_tools.Count} tools registered");

		while(!_shuttingDown) {
			TcpClient client;
			try {
				client = listener.AcceptTcpClient();
			} catch(Exception ex) {
				WriteLog($"[mcp] accept failed: {ex.Message}");
				break;
			}
			WriteLog($"[mcp] client connected");
			HandleClient(client);
			WriteLog($"[mcp] client disconnected");
		}

		try {
			listener.Stop();
		} catch { }
		WriteLog("[mcp] server exiting");
	}

	private void HandleClient(TcpClient client)
	{
		using(client)
		using(NetworkStream stream = client.GetStream()) {
			_in = new StreamReader(stream, new UTF8Encoding(false));
			_out = new StreamWriter(stream, new UTF8Encoding(false)) {
				AutoFlush = true,
				NewLine = "\n",
			};

			// Kick off the event-drain pump. Reads hooks events from the
			// emulator side via a small buffer and emits MCP notifications
			// on this socket. Lifecycle is tied to the client: the thread
			// exits as soon as _clientConnected flips false below.
			Interop.DebugApi.McpResetHooks();  // defensive: start each session clean
			var drainCts = new CancellationTokenSource();
			var drainThread = new Thread(() => DrainLoop(drainCts.Token)) {
				IsBackground = true,
				Name = "mcp-event-drain",
			};
			drainThread.Start();

			try {
				while(!_shuttingDown) {
					string? line;
					try {
						line = _in.ReadLine();
					} catch(Exception ex) {
						WriteLog($"[mcp] read error: {ex.Message}");
						break;
					}
					if(line == null) {
						break;
					}
					if(line.Length == 0) {
						continue;
					}
					HandleMessage(line);
				}
			} finally {
				// Shut down the drain thread before tearing down socket fields
				// so we can't race a notification write against a null writer.
				drainCts.Cancel();
				drainThread.Join(1000);
				Interop.DebugApi.McpResetHooks();  // don't leak hooks to next client
			}
		}
		_in = null;
		_out = null;
	}

	private void DrainLoop(CancellationToken cancel)
	{
		// Buffer sized to amortize the P/Invoke cost per drain call. 256 events
		// is ~5KB marshalled and hits well-behaved hooks once per a few frames.
		var buf = new Interop.McpHookEvent[256];
		while(!cancel.IsCancellationRequested) {
			int n;
			try {
				n = Interop.DebugApi.McpDrainEvents(buf, buf.Length);
			} catch {
				// If the debugger isn't alive (e.g. Emu shutting down), bail.
				break;
			}

			for(int i = 0; i < n; i++) {
				SendHookNotification(buf[i]);
			}

			// Adaptive sleep: if we just drained a full buffer, assume more
			// are queued and loop immediately. Otherwise sleep briefly so we
			// don't hot-poll an idle session.
			if(n < buf.Length) {
				try {
					cancel.WaitHandle.WaitOne(2);
				} catch {
					break;
				}
			}
		}
	}

	private void SendHookNotification(Interop.McpHookEvent evt)
	{
		// Notification per MCP spec: no `id`, so it's not a response.
		// Mirror the tool-result content envelope so clients can use the same
		// deserialize path, but flag as a notification by routing via 'method'.
		var msg = new JsonObject {
			["jsonrpc"] = "2.0",
			["method"] = "notifications/mesen/hookFired",
			["params"] = new JsonObject {
				["handle"] = evt.Handle,
				["kind"] = evt.Kind.ToString(),
				["cpuType"] = evt.Cpu.ToString(),
				["address"] = evt.Address,
				["value"] = evt.Value,
				["frame"] = evt.FrameNumber,
			},
		};
		WriteLine(msg);
	}

	private void HandleMessage(string raw)
	{
		JsonNode? msg;
		try {
			msg = JsonNode.Parse(raw);
		} catch(Exception ex) {
			SendError(null, -32700, "Parse error: " + ex.Message);
			return;
		}
		if(msg == null) {
			SendError(null, -32600, "Invalid Request: empty message");
			return;
		}

		JsonNode? idNode = msg["id"];
		string? method = msg["method"]?.GetValue<string>();
		JsonNode? parameters = msg["params"];

		if(method == null) {
			// No method means it's a response to something we sent — we don't
			// send requests, so ignore / warn.
			WriteLog("[mcp] dropping message without 'method'");
			return;
		}

		try {
			switch(method) {
				case "initialize":
					SendResult(idNode, HandleInitialize(parameters));
					break;
				case "initialized":
				case "notifications/initialized":
					// Notification, no response.
					break;
				case "tools/list":
					SendResult(idNode, HandleToolsList());
					break;
				case "tools/call":
					SendResult(idNode, HandleToolsCall(parameters));
					break;
				case "shutdown":
					_shuttingDown = true;
					SendResult(idNode, new JsonObject());
					break;
				default:
					SendError(idNode, -32601, "Method not found: " + method);
					break;
			}
		} catch(McpException mex) {
			SendError(idNode, mex.Code, mex.Message);
		} catch(Exception ex) {
			SendError(idNode, -32603, "Internal error: " + ex.Message);
		}
	}

	private JsonNode HandleInitialize(JsonNode? parameters)
	{
		return new JsonObject {
			["protocolVersion"] = "2024-11-05",
			["serverInfo"] = new JsonObject {
				["name"] = "mesen",
				["version"] = GetMesenVersion(),
			},
			["capabilities"] = new JsonObject {
				["tools"] = new JsonObject(),
			},
		};
	}

	private JsonNode HandleToolsList()
	{
		var tools = new JsonArray();
		foreach(var tool in McpTools.Descriptions) {
			tools.Add(tool.ToJson());
		}
		return new JsonObject { ["tools"] = tools };
	}

	private JsonNode HandleToolsCall(JsonNode? parameters)
	{
		if(parameters == null) {
			throw new McpException(-32602, "Missing params");
		}
		string? name = parameters["name"]?.GetValue<string>();
		if(name == null) {
			throw new McpException(-32602, "Missing params.name");
		}
		if(!_tools.TryGetValue(name, out var handler)) {
			throw new McpException(-32602, "Unknown tool: " + name);
		}
		JsonNode? args = parameters["arguments"];

		JsonNode payload;
		try {
			payload = handler(args);
		} catch(McpException) {
			throw;
		} catch(Exception ex) {
			// Tool-level failures surface as a content-flagged result, not a
			// JSON-RPC error, so the model sees the error text as a tool
			// output and can react to it.
			return new JsonObject {
				["content"] = new JsonArray {
					new JsonObject {
						["type"] = "text",
						["text"] = $"tool {name} raised {ex.GetType().Name}: {ex.Message}",
					},
				},
				["isError"] = true,
			};
		}

		// Tools return their structured result as a JSON object; wrap it in
		// the MCP content envelope as a single JSON-encoded text item. MCP
		// clients expect content[].text; structured data travels inside that
		// text body so the model sees it verbatim.
		return new JsonObject {
			["content"] = new JsonArray {
				new JsonObject {
					["type"] = "text",
					["text"] = payload.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
				},
			},
		};
	}

	private Dictionary<string, Func<JsonNode?, JsonNode>> BuildToolTable()
	{
		return new Dictionary<string, Func<JsonNode?, JsonNode>> {
			["ping"] = McpTools.Ping,
			["get_state"] = McpTools.GetState,
			["pause"] = McpTools.Pause,
			["resume"] = McpTools.Resume,
			["run_frames"] = McpTools.RunFrames,
			["read_memory"] = McpTools.ReadMemory,
			["write_memory"] = McpTools.WriteMemory,
			["take_screenshot"] = McpTools.TakeScreenshot,
			["save_state"] = McpTools.SaveState,
			["load_state"] = McpTools.LoadState,
			["set_input"] = McpTools.SetInput,
			["get_ppu_state"] = McpTools.GetPpuState,
			["add_exec_hook"] = McpTools.AddExecHook,
			["remove_hook"] = McpTools.RemoveHook,
			["list_hooks"] = McpTools.ListHooks,
			["hook_diag"] = McpTools.HookDiag,
		};
	}

	private void SendResult(JsonNode? id, JsonNode result)
	{
		var response = new JsonObject {
			["jsonrpc"] = "2.0",
			["id"] = id?.DeepClone(),
			["result"] = result,
		};
		WriteLine(response);
	}

	private void SendError(JsonNode? id, int code, string message)
	{
		var response = new JsonObject {
			["jsonrpc"] = "2.0",
			["id"] = id?.DeepClone(),
			["error"] = new JsonObject {
				["code"] = code,
				["message"] = message,
			},
		};
		WriteLine(response);
	}

	private void WriteLine(JsonNode node)
	{
		TextWriter? writer = _out;
		if(writer == null) {
			return;
		}
		lock(writer) {
			writer.WriteLine(node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
		}
	}

	private static void WriteLog(string message)
	{
		// Logs must NOT go to stdout (that's the JSON-RPC channel). Use
		// stderr so the parent process can still see them on demand.
		try {
			Console.Error.WriteLine(message);
		} catch { }
	}

	private static string GetMesenVersion()
	{
		try {
			var asm = System.Reflection.Assembly.GetExecutingAssembly();
			var ver = asm.GetName().Version;
			return ver?.ToString() ?? "0.0.0";
		} catch {
			return "0.0.0";
		}
	}
}

internal sealed class McpException : Exception
{
	public int Code { get; }
	public McpException(int code, string message) : base(message)
	{
		Code = code;
	}
}
