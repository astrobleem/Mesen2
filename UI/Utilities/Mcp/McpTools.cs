using Mesen.Interop;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Mesen.Utilities.Mcp;

/// <summary>
/// MCP tool implementations. Each tool maps an arguments JsonNode to a
/// result JsonNode. Tools throw McpException for protocol-level failures
/// (bad args); uncaught exceptions are caught in McpServer and surfaced as
/// isError tool results so the caller sees the failure as output.
///
/// Tools are intentionally small and composable — the caller orchestrates
/// multi-step workflows, not the tool layer. If a workflow needs 4 calls,
/// that's fine; the session is persistent and round-trips are cheap.
/// </summary>
internal static class McpTools
{
	public static readonly IReadOnlyList<McpToolDesc> Descriptions = new List<McpToolDesc> {
		new("ping",
			"Echo back. Use to verify the MCP session is alive.",
			null),

		new("get_state",
			"Snapshot of emulator state: isRunning, isPaused. Emulation state "
				+ "reads are only consistent if the emulator is paused — use pause "
				+ "before a sequence of read_memory calls to get a coherent snapshot.",
			null),

		new("pause",
			"Pause emulation. All read_* tools become race-free while paused. "
				+ "Follow with resume or run_frames to advance.",
			null),

		new("resume",
			"Resume emulation at full speed. Reads are valid but may see a moving "
				+ "target across multiple calls.",
			null),

		new("run_frames",
			"Advance emulation by exactly N frames, then pause again. Deterministic "
				+ "alternative to sleeping in the client — no time-based races.",
			BuildRunFramesSchema()),

		new("read_memory",
			"Read N bytes from a memory region. Returns hex-encoded bytes. "
				+ "Pause first for a race-free read.",
			BuildReadMemorySchema()),

		new("write_memory",
			"Write N bytes (hex-encoded) to a memory region. Use for WRAM pokes "
				+ "(room transitions, actor position seeds, etc). Pause the emulator "
				+ "first if you need the write to take effect on a specific frame.",
			BuildWriteMemorySchema()),

		new("take_screenshot",
			"Capture the current PPU frame as a PNG. Returns path (default) or "
				+ "base64-encoded bytes. Pause first or accept mid-render.",
			BuildScreenshotSchema()),

		new("save_state",
			"Save emulator state to a named slot or file path. Pair with load_state "
				+ "to checkpoint expensive boots (skip intros permanently).",
			BuildSaveLoadSchema()),

		new("load_state",
			"Load emulator state from a named slot or file path.",
			BuildSaveLoadSchema()),
	};

	private static JsonNode BuildRunFramesSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("count"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["count"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Number of frames to advance (1..1000000)",
				},
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildWriteMemorySchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("memoryType"));
		required.Add(JsonValue.Create("address"));
		required.Add(JsonValue.Create("hex"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["memoryType"] = new JsonObject { ["type"] = "string" },
				["address"] = new JsonObject { ["type"] = "integer" },
				["hex"] = new JsonObject {
					["type"] = "string",
					["description"] = "Hex-encoded bytes, no separators (e.g. 'deadbeef')",
				},
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildScreenshotSchema()
	{
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["format"] = new JsonObject {
					["type"] = "string",
					["description"] = "'path' (default; writes PNG to Screenshots folder, returns path) or 'base64' (inline PNG bytes)",
				},
			},
		};
	}

	private static JsonNode BuildSaveLoadSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("path"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["path"] = new JsonObject {
					["type"] = "string",
					["description"] = "File path for the save-state .mss file",
				},
			},
			["required"] = required,
		};
	}

	// Build the read_memory JSON schema imperatively. JsonArray's collection-
	// initializer overload takes JsonNode params and ends up invoking the
	// generic Add<T> path at runtime, which trips IL2026 and throws under
	// trimming/AOT. Using explicit JsonValue.Create calls sidesteps the whole
	// dynamic-codegen question.
	private static JsonNode BuildReadMemorySchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("memoryType"));
		required.Add(JsonValue.Create("address"));
		required.Add(JsonValue.Create("length"));

		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["memoryType"] = new JsonObject {
					["type"] = "string",
					["description"] = "One of: snesMemory (CPU bus), snesWorkRam, snesVideoRam, snesCgRam, snesSpriteRam, snesPrgRom, sa1Memory",
				},
				["address"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Start address",
				},
				["length"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Number of bytes to read (max 65536)",
				},
			},
			["required"] = required,
		};
	}

	public static JsonNode Ping(JsonNode? args)
	{
		return new JsonObject {
			["pong"] = true,
			["echo"] = args?.DeepClone(),
		};
	}

	public static JsonNode GetState(JsonNode? args)
	{
		// TODO expose actual frame count once we expose GetFrameCount via
		// InteropDLL; IsRunning/IsPaused are already exported.
		return new JsonObject {
			["isRunning"] = EmuApi.IsRunning(),
			["isPaused"] = EmuApi.IsPaused(),
		};
	}

	public static JsonNode Pause(JsonNode? args)
	{
		EmuApi.Pause();
		// EmuApi.Pause queues the request; the emulator only actually halts
		// on the next frame boundary. Busy-wait briefly so the next read
		// sees a consistent state — callers would otherwise need to poll
		// IsPaused themselves after every pause.
		for(int i = 0; i < 200; i++) {
			if(EmuApi.IsPaused()) break;
			System.Threading.Thread.Sleep(5);
		}
		return new JsonObject { ["paused"] = EmuApi.IsPaused() };
	}

	public static JsonNode Resume(JsonNode? args)
	{
		EmuApi.Resume();
		return new JsonObject { ["paused"] = false };
	}

	public static JsonNode RunFrames(JsonNode? args)
	{
		if(args == null) {
			throw new McpException(-32602, "run_frames requires arguments");
		}
		long count = args["count"]?.GetValue<long>() ?? throw new McpException(-32602, "missing arg: count");
		if(count <= 0 || count > 1_000_000) {
			throw new McpException(-32602, "count out of range (1..1000000)");
		}

		// Sleep-and-pause. Not frame-accurate — max-speed mode races past
		// N frames in much less wall-clock than 60 FPS would imply. Good
		// enough for "advance a bit" use cases; if a tool later needs exact
		// frame counts, add a Debug::Step hook.
		EmuApi.Resume();
		double estMs = count * (1000.0 / 60.0);
		System.Threading.Thread.Sleep((int)Math.Min(estMs * 1.2 + 20, 60_000));
		EmuApi.Pause();
		for(int i = 0; i < 200; i++) {
			if(EmuApi.IsPaused()) break;
			System.Threading.Thread.Sleep(5);
		}

		return new JsonObject {
			["requested"] = count,
			["isPaused"] = EmuApi.IsPaused(),
		};
	}

	public static JsonNode WriteMemory(JsonNode? args)
	{
		if(args == null) {
			throw new McpException(-32602, "write_memory requires arguments");
		}
		string memTypeStr = RequireString(args, "memoryType");
		uint address = RequireUInt(args, "address");
		string hex = RequireString(args, "hex");
		byte[] bytes;
		try {
			bytes = Convert.FromHexString(hex);
		} catch {
			throw new McpException(-32602, "hex is not a valid hex string");
		}

		MemoryType memType = ParseMemoryType(memTypeStr);
		for(uint i = 0; i < bytes.Length; i++) {
			DebugApi.SetMemoryValue(memType, address + i, bytes[i]);
		}

		return new JsonObject {
			["memoryType"] = memTypeStr,
			["address"] = address,
			["length"] = bytes.Length,
		};
	}

	public static JsonNode TakeScreenshot(JsonNode? args)
	{
		string format = (args?["format"]?.GetValue<string>() ?? "path").ToLowerInvariant();

		// EmuApi.TakeScreenshot writes to the configured Screenshots folder
		// using a timestamped filename. We capture the newest file that
		// appears as our return path. Simple, avoids framebuffer plumbing.
		string dir = Mesen.Config.ConfigManager.ScreenshotFolder;
		HashSet<string> before;
		try {
			before = new HashSet<string>(System.IO.Directory.GetFiles(dir));
		} catch {
			before = new HashSet<string>();
		}

		EmuApi.TakeScreenshot();

		// TakeScreenshot is async-queued; give it up to 2s to land.
		string? path = null;
		for(int i = 0; i < 40; i++) {
			System.Threading.Thread.Sleep(50);
			try {
				foreach(string f in System.IO.Directory.GetFiles(dir, "*.png")) {
					if(!before.Contains(f)) {
						path = f;
						break;
					}
				}
			} catch { }
			if(path != null) break;
		}
		if(path == null) {
			throw new McpException(-32603, "TakeScreenshot did not produce a file within 2s");
		}

		var result = new JsonObject {
			["path"] = path,
		};

		if(format == "base64") {
			byte[] bytes = System.IO.File.ReadAllBytes(path);
			result["base64"] = Convert.ToBase64String(bytes);
			result["bytes"] = bytes.Length;
		}
		return result;
	}

	public static JsonNode SaveState(JsonNode? args)
	{
		if(args == null) {
			throw new McpException(-32602, "save_state requires arguments");
		}
		string path = RequireString(args, "path");
		EmuApi.SaveStateFile(path);
		return new JsonObject { ["path"] = path };
	}

	public static JsonNode LoadState(JsonNode? args)
	{
		if(args == null) {
			throw new McpException(-32602, "load_state requires arguments");
		}
		string path = RequireString(args, "path");
		EmuApi.LoadStateFile(path);
		return new JsonObject { ["path"] = path };
	}

	public static JsonNode ReadMemory(JsonNode? args)
	{
		if(args == null) {
			throw new McpException(-32602, "read_memory requires arguments");
		}
		string memTypeStr = RequireString(args, "memoryType");
		uint address = RequireUInt(args, "address");
		uint length = RequireUInt(args, "length");
		if(length == 0) {
			return new JsonObject {
				["memoryType"] = memTypeStr,
				["address"] = address,
				["length"] = 0,
				["hex"] = "",
			};
		}
		if(length > 65536) {
			throw new McpException(-32602, "length > 65536");
		}

		MemoryType memType = ParseMemoryType(memTypeStr);
		byte[] buffer = DebugApi.GetMemoryValues(memType, address, address + length - 1);

		return new JsonObject {
			["memoryType"] = memTypeStr,
			["address"] = address,
			["length"] = length,
			["hex"] = Convert.ToHexString(buffer),
		};
	}

	// --- helpers ------------------------------------------------------------

	private static string RequireString(JsonNode args, string key)
	{
		var node = args[key];
		if(node == null) {
			throw new McpException(-32602, $"missing arg: {key}");
		}
		try {
			return node.GetValue<string>();
		} catch {
			throw new McpException(-32602, $"arg {key} must be a string");
		}
	}

	private static uint RequireUInt(JsonNode args, string key)
	{
		var node = args[key];
		if(node == null) {
			throw new McpException(-32602, $"missing arg: {key}");
		}
		try {
			long v = node.GetValue<long>();
			if(v < 0 || v > uint.MaxValue) {
				throw new McpException(-32602, $"arg {key} out of range: {v}");
			}
			return (uint)v;
		} catch(McpException) {
			throw;
		} catch {
			throw new McpException(-32602, $"arg {key} must be an integer");
		}
	}

	private static MemoryType ParseMemoryType(string name)
	{
		// Accept the exact Lua memType names for script portability, plus
		// the raw enum names for callers that think in C#/C++ terms.
		return name switch {
			"snesMemory" => MemoryType.SnesMemory,
			"snesWorkRam" => MemoryType.SnesWorkRam,
			"snesVideoRam" => MemoryType.SnesVideoRam,
			"snesCgRam" => MemoryType.SnesCgRam,
			"snesSpriteRam" => MemoryType.SnesSpriteRam,
			"snesPrgRom" => MemoryType.SnesPrgRom,
			"sa1Memory" => MemoryType.Sa1Memory,
			"sa1IRam" => MemoryType.Sa1InternalRam,
			_ when Enum.TryParse<MemoryType>(name, ignoreCase: true, out var parsed) => parsed,
			_ => throw new McpException(-32602, "unknown memoryType: " + name),
		};
	}
}

internal sealed class McpToolDesc
{
	public string Name { get; }
	public string Description { get; }
	public JsonNode? InputSchema { get; }

	public McpToolDesc(string name, string description, JsonNode? inputSchema)
	{
		Name = name;
		Description = description;
		InputSchema = inputSchema;
	}

	public JsonNode ToJson()
	{
		var obj = new JsonObject {
			["name"] = Name,
			["description"] = Description,
		};
		// Input schema is required by the MCP spec even for no-arg tools;
		// emit an empty object schema in that case so clients stay happy.
		obj["inputSchema"] = InputSchema?.DeepClone() ?? new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject(),
		};
		return obj;
	}
}
