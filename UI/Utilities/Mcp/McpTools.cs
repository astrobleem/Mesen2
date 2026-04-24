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
			"Snapshot of emulator state: isRunning, isPaused, frameCount, romPath. "
				+ "Frame count is authoritative — scripts no longer need to count their own.",
			null),

		new("read_memory",
			"Read N bytes from a memory region. Returns hex-encoded bytes. "
				+ "Prefer this over ad-hoc rd8/rd16 helpers — single call, reliable, "
				+ "no per-frame-callback gymnastics.",
			BuildReadMemorySchema()),
	};

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
