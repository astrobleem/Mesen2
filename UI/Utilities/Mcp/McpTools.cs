using Mesen.Debugger;
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

		new("set_input",
			"Inject controller state for the next N frames. Buttons are bit-OR "
				+ "of: a=1,b=2,select=4,start=8,up=16,down=32,left=64,right=128 and "
				+ "SNES-only x=256,l=512,r=1024,y=2048. Use when scripts need to "
				+ "drive past the title screen or trigger script-driven transitions "
				+ "the way the real controller does.",
			BuildSetInputSchema()),

		new("get_ppu_state",
			"Return PPU register snapshot: forced-blank flag, brightness, BG mode, "
				+ "main/sub screen layer enable masks (bit0=BG1..bit4=OBJ), per-layer "
				+ "scroll + tile/tilemap addrs, window config. Essential for diagnosing "
				+ "'black scanlines' / 'missing layer' bugs without eyeball + guesswork.",
			null),

		new("add_exec_hook",
			"Fire on CPU instruction execution at address (or in [address..endAddress]). "
				+ "Returns a handle; remove_hook to detach. Per-fire the server sends "
				+ "notifications/mesen/hookFired with handle, address, value, frame. "
				+ "Optional matchValue+matchValueMask filters server-side so high-volume "
				+ "PCs don't flood the socket.",
			BuildAddHookSchema(includeValueMatch: true)),

		new("add_read_hook",
			"Fire on CPU memory reads at address (or [address..endAddress]). The "
				+ "value field of the notification is the byte read. matchValue/Mask "
				+ "supports the common 'fire when value == X' pattern.",
			BuildAddHookSchema(includeValueMatch: true)),

		new("add_write_hook",
			"Fire on CPU memory writes. value = byte written. matchValue/Mask same as "
				+ "the other hook tools.",
			BuildAddHookSchema(includeValueMatch: true)),

		new("remove_hook",
			"Detach a previously-registered hook by handle.",
			BuildRemoveHookSchema()),

		new("list_hooks",
			"List currently-registered hooks. Snapshot only; hooks can fire between "
				+ "the list call and the response.",
			null),

		new("hook_diag",
			"Diagnostic counters: total calls into the MCP hook hot-path + total "
				+ "address-range matches since reset. Lets you confirm the hot-path "
				+ "is alive even when no hook address has fired yet.",
			null),

		new("lookup_symbol",
			"Resolve symbol names from a WLA-DX-format sym file. Pass the file path "
				+ "(typically build/SuperMonkeyIsland.sym for SNES projects) and a "
				+ "regex pattern. Returns matches with their bank:offset + 24-bit ROM "
				+ "address ($C0|offset for HiROM bank 0). Caches the parsed file.",
			BuildLookupSymbolSchema()),

		new("disassemble",
			"Return N disassembled instructions starting at address. Wraps Mesen's "
				+ "GetDisassemblyOutput. Useful for one-shot 'what's at this PC' rather "
				+ "than dragging out a hex dump.",
			BuildDisassembleSchema()),

		new("run_until",
			"Resume emulation until any of (a) up to N frames pass, or (b) a hook fires. "
				+ "Returns the trigger reason and (if a hook fired) which handle. "
				+ "Atomic 'pause -> set hook -> resume -> wait -> pause' so callers "
				+ "don't have to roll the state machine themselves.",
			BuildRunUntilSchema()),

		new("crop_screenshot",
			"Capture a screenshot, then crop a region (x, y, w, h). Returns path or "
				+ "inline base64 like take_screenshot. For UI region checks where "
				+ "the full frame is wasted bytes.",
			BuildCropScreenshotSchema()),

		new("save_state_slot",
			"Save emulator state to numbered slot (0..9). Cheap checkpoint without "
				+ "managing file paths. Pair with load_state_slot to fast-forward past "
				+ "expensive boots.",
			BuildSlotSchema()),

		new("load_state_slot",
			"Load emulator state from numbered slot.",
			BuildSlotSchema()),

		new("read_dma_state",
			"Snapshot the 8 DMA/HDMA channel registers ($4300-$437F). Returns each "
				+ "channel's control / target / source / count / table addr. Critical "
				+ "for diagnosing per-scanline register effects (BG layer enables, "
				+ "scroll registers, palette ramps).",
			null),

		new("add_frame_hook",
			"Fire a notification once per emulator frame. Replaces the per-frame "
				+ "polling loop most diagnostic Lua scripts open with. The notification "
				+ "carries the frame number; use it to pace your client without sleeping.",
			BuildAddFrameHookSchema()),

		new("reset_emulator",
			"Soft-reset the emulator (equivalent to the SNES reset button). State "
				+ "wiped; the ROM stays loaded. Use to start each MCP-driven test "
				+ "from a known initial state without respawning Mesen.",
			null),

		new("record_audio",
			"Start recording emulator audio to a WAV file. Pair with stop_audio. "
				+ "Captures the same sample stream the user would hear, so client-side "
				+ "FFT analysis ('which voices are active', 'is the song playing the "
				+ "right pitch') becomes possible without ears.",
			BuildRecordAudioSchema()),

		new("stop_audio",
			"Stop the active audio recording.",
			null),

		new("get_audio_state",
			"Snapshot SPC700 + S-DSP register state. SPC700 fields = CPU registers "
				+ "(PC, A/X/Y, SP, flags). DSP fields include the 128-byte register "
				+ "block — voice key-on/off, volume L/R per voice, pitch, ADSR envelope, "
				+ "etc. Lets you ask 'what is the audio engine doing right now' rather "
				+ "than 'what does the song sound like'.",
			null),
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

	private static JsonNode BuildLookupSymbolSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("symFile"));
		required.Add(JsonValue.Create("pattern"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["symFile"] = new JsonObject {
					["type"] = "string",
					["description"] = "Absolute path to a WLA-DX .sym file",
				},
				["pattern"] = new JsonObject {
					["type"] = "string",
					["description"] = "Regex pattern matched against symbol names. Use '^name$' for exact match.",
				},
				["maxResults"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Cap on results returned (default 64)",
				},
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildDisassembleSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("address"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["address"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Start address (CPU bus, e.g. 0xC08000)",
				},
				["count"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Number of lines (default 16)",
				},
				["cpuType"] = new JsonObject {
					["type"] = "string",
					["description"] = "CPU type (default 'Snes')",
				},
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildRecordAudioSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("path"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["path"] = new JsonObject {
					["type"] = "string",
					["description"] = "Output .wav path. Existing files are overwritten.",
				},
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildAddFrameHookSchema()
	{
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["everyN"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Fire only every N-th frame (default 1, every frame). Higher values reduce notification volume.",
				},
				["cpuType"] = new JsonObject {
					["type"] = "string",
					["description"] = "CPU type the frame is associated with (default 'Snes'). Mostly cosmetic — frames are global.",
				},
			},
		};
	}

	private static JsonNode BuildCropScreenshotSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("x"));
		required.Add(JsonValue.Create("y"));
		required.Add(JsonValue.Create("width"));
		required.Add(JsonValue.Create("height"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["x"] = new JsonObject { ["type"] = "integer" },
				["y"] = new JsonObject { ["type"] = "integer" },
				["width"] = new JsonObject { ["type"] = "integer" },
				["height"] = new JsonObject { ["type"] = "integer" },
				["format"] = new JsonObject { ["type"] = "string", ["description"] = "'path' or 'base64'" },
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildSlotSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("slot"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["slot"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Slot index 0..9",
				},
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildRunUntilSchema()
	{
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["maxFrames"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Frame budget; emulation pauses after this many frames if no hook fires (default 600).",
				},
				["hookHandle"] = new JsonObject {
					["type"] = "integer",
					["description"] = "An existing hook handle to wait on. If 0/missing, run for maxFrames and return.",
				},
			},
		};
	}

	private static JsonNode BuildAddHookSchema(bool includeValueMatch)
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("address"));
		var props = new JsonObject {
			["address"] = new JsonObject {
				["type"] = "integer",
				["description"] = "Start address (inclusive)",
			},
			["endAddress"] = new JsonObject {
				["type"] = "integer",
				["description"] = "End address (inclusive). Defaults to address for a single-byte/PC hook.",
			},
			["cpuType"] = new JsonObject {
				["type"] = "string",
				["description"] = "CPU type (default 'Snes'; other valid: 'Sa1', 'Spc', 'Gameboy', etc).",
			},
		};
		if(includeValueMatch) {
			props["matchValue"] = new JsonObject {
				["type"] = "integer",
				["description"] = "Optional value match. Hook only fires when (observed & matchValueMask) == (matchValue & matchValueMask).",
			};
			props["matchValueMask"] = new JsonObject {
				["type"] = "integer",
				["description"] = "Mask for matchValue. 0 disables value matching (fire on every hit). 0xFF for byte-exact.",
			};
		}
		return new JsonObject {
			["type"] = "object",
			["properties"] = props,
			["required"] = required,
		};
	}

	private static JsonNode BuildRemoveHookSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("handle"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["handle"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Handle returned from add_exec_hook.",
				},
			},
			["required"] = required,
		};
	}

	private static JsonNode BuildSetInputSchema()
	{
		var required = new JsonArray();
		required.Add(JsonValue.Create("port"));
		required.Add(JsonValue.Create("buttons"));
		required.Add(JsonValue.Create("frames"));
		return new JsonObject {
			["type"] = "object",
			["properties"] = new JsonObject {
				["port"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Controller port (0 = player 1)",
				},
				["buttons"] = new JsonObject {
					["type"] = "integer",
					["description"] = "Bitmask of buttons (e.g. 8 = start)",
				},
				["frames"] = new JsonObject {
					["type"] = "integer",
					["description"] = "How many frames to hold the input; emulation advances during this window",
				},
			},
			["required"] = required,
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
		return new JsonObject {
			["isRunning"] = EmuApi.IsRunning(),
			["isPaused"] = EmuApi.IsPaused(),
			["frameCount"] = EmuApi.GetFrameCount(),
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

	public static JsonNode CropScreenshot(JsonNode? args)
	{
		if(args == null) throw new McpException(-32602, "crop_screenshot requires arguments");
		int x = (int)RequireUInt(args, "x");
		int y = (int)RequireUInt(args, "y");
		int w = (int)RequireUInt(args, "width");
		int h = (int)RequireUInt(args, "height");
		if(w <= 0 || h <= 0) throw new McpException(-32602, "width and height must be > 0");
		string format = (args["format"]?.GetValue<string>() ?? "path").ToLowerInvariant();

		// Reuse the regular screenshot path so we get the same async-flush
		// behaviour, then crop the resulting PNG.
		var raw = (JsonObject)TakeScreenshot(new JsonObject { ["format"] = "path" });
		string srcPath = raw["path"]!.GetValue<string>();

		using var src = SkiaSharp.SKBitmap.Decode(srcPath);
		if(src == null) {
			throw new McpException(-32603, "failed to decode screenshot: " + srcPath);
		}
		// Clamp the crop rect to the image bounds rather than throwing —
		// callers usually know the SNES is 256x224 but small mistakes are
		// cheap to fix on the way out.
		int clampW = Math.Min(w, src.Width - x);
		int clampH = Math.Min(h, src.Height - y);
		if(clampW <= 0 || clampH <= 0) {
			throw new McpException(-32602, $"crop region ({x},{y},{w}x{h}) is outside the {src.Width}x{src.Height} screenshot");
		}

		using var dst = new SkiaSharp.SKBitmap(clampW, clampH);
		using(var canvas = new SkiaSharp.SKCanvas(dst)) {
			canvas.DrawBitmap(src,
				new SkiaSharp.SKRect(x, y, x + clampW, y + clampH),
				new SkiaSharp.SKRect(0, 0, clampW, clampH));
		}

		string cropPath = System.IO.Path.ChangeExtension(srcPath, ".crop.png");
		using(var stream = System.IO.File.OpenWrite(cropPath))
		using(var img = SkiaSharp.SKImage.FromBitmap(dst))
		using(var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100)) {
			data.SaveTo(stream);
		}

		var result = new JsonObject {
			["path"] = cropPath,
			["width"] = clampW,
			["height"] = clampH,
		};
		if(format == "base64") {
			byte[] bytes = System.IO.File.ReadAllBytes(cropPath);
			result["base64"] = Convert.ToBase64String(bytes);
			result["bytes"] = bytes.Length;
		}
		return result;
	}

	public static JsonNode SaveStateSlot(JsonNode? args)
	{
		if(args == null) throw new McpException(-32602, "save_state_slot requires arguments");
		uint slot = RequireUInt(args, "slot");
		if(slot > 9) throw new McpException(-32602, "slot out of range (0..9)");
		EmuApi.SaveState(slot);
		return new JsonObject { ["slot"] = slot };
	}

	public static JsonNode LoadStateSlot(JsonNode? args)
	{
		if(args == null) throw new McpException(-32602, "load_state_slot requires arguments");
		uint slot = RequireUInt(args, "slot");
		if(slot > 9) throw new McpException(-32602, "slot out of range (0..9)");
		EmuApi.LoadState(slot);
		return new JsonObject { ["slot"] = slot };
	}

	public static JsonNode AddFrameHook(JsonNode? args)
	{
		args ??= new JsonObject();
		uint everyN = (uint)((args["everyN"]?.GetValue<long>()) ?? 1);
		if(everyN == 0) throw new McpException(-32602, "everyN must be > 0");
		string cpuStr = args["cpuType"]?.GetValue<string>() ?? "Snes";
		if(!Enum.TryParse<CpuType>(cpuStr, ignoreCase: true, out var cpu)) {
			throw new McpException(-32602, "unknown cpuType: " + cpuStr);
		}
		// Frame number ranges over uint32; address-range check passes if we
		// give a hook the full uint32 span. everyN is implemented via the
		// value-match: matchValueMask = (everyN-1) and matchValue = 0 only
		// matches when (frame & mask) == 0.
		uint mask = everyN - 1;
		bool isPow2 = everyN != 0 && (everyN & mask) == 0;
		if(!isPow2) {
			// Fallback: no value match, fire every frame and let the client filter.
			mask = 0;
		}
		int handle = DebugApi.McpAddHook((byte)McpHookKind.Frame, cpu, 0, uint.MaxValue, 0, mask);
		return new JsonObject {
			["handle"] = handle,
			["kind"] = "Frame",
			["everyN"] = isPow2 ? (long)everyN : 1,
			["cpuType"] = cpu.ToString(),
		};
	}

	public static JsonNode RecordAudio(JsonNode? args)
	{
		if(args == null) throw new McpException(-32602, "record_audio requires arguments");
		string path = RequireString(args, "path");
		Mesen.Interop.RecordApi.WaveRecord(path);
		return new JsonObject { ["path"] = path, ["recording"] = true };
	}

	public static JsonNode StopAudio(JsonNode? args)
	{
		Mesen.Interop.RecordApi.WaveStop();
		return new JsonObject { ["recording"] = false };
	}

	public static JsonNode GetAudioState(JsonNode? args)
	{
		var state = DebugApi.GetConsoleState<Mesen.Interop.SnesState>(ConsoleType.Snes);

		var spc = state.Spc;
		var dsp = state.Dsp;

		// Decode the per-voice DSP register block. Each voice has 8 register
		// slots at offsets x0..x9; voice index in high nibble. Surface the
		// most useful subset for "is the music playing": volume L/R, pitch,
		// source #, ADSR1/ADSR2, gain, envelope, current output.
		var voices = new JsonArray();
		for(int v = 0; v < 8; v++) {
			int b = v * 0x10;
			byte volL  = dsp.ExternalRegs[b + 0x00];
			byte volR  = dsp.ExternalRegs[b + 0x01];
			ushort pitch = (ushort)(dsp.ExternalRegs[b + 0x02] | (dsp.ExternalRegs[b + 0x03] << 8));
			byte src   = dsp.ExternalRegs[b + 0x04];
			byte adsr1 = dsp.ExternalRegs[b + 0x05];
			byte adsr2 = dsp.ExternalRegs[b + 0x06];
			byte gain  = dsp.ExternalRegs[b + 0x07];
			byte envx  = dsp.ExternalRegs[b + 0x08];
			byte outx  = dsp.ExternalRegs[b + 0x09];
			voices.Add(new JsonObject {
				["voice"] = v,
				["volL"] = (sbyte)volL,
				["volR"] = (sbyte)volR,
				["pitch"] = pitch,
				["sampleSrc"] = src,
				["adsr1"] = adsr1,
				["adsr2"] = adsr2,
				["gain"] = gain,
				["envelope"] = envx,
				["currentOutput"] = (sbyte)outx,
			});
		}

		// Master flags. $0C = MVOLL, $1C = MVOLR, $2C = EVOLL, $3C = EVOLR,
		// $4C = KON (key on), $5C = KOF (key off), $6C = FLG (mute/reset/echo),
		// $7C = ENDX (which voices ended).
		return new JsonObject {
			["spc"] = new JsonObject {
				["pc"] = spc.PC,
				["a"] = spc.A,
				["x"] = spc.X,
				["y"] = spc.Y,
				["sp"] = spc.SP,
				["cycle"] = spc.Cycle,
			},
			["dsp"] = new JsonObject {
				["mainVolL"] = (sbyte)dsp.ExternalRegs[0x0C],
				["mainVolR"] = (sbyte)dsp.ExternalRegs[0x1C],
				["echoVolL"] = (sbyte)dsp.ExternalRegs[0x2C],
				["echoVolR"] = (sbyte)dsp.ExternalRegs[0x3C],
				["keyOn"] = dsp.ExternalRegs[0x4C],
				["keyOff"] = dsp.ExternalRegs[0x5C],
				["flg"] = dsp.ExternalRegs[0x6C],
				["voicesEnded"] = dsp.ExternalRegs[0x7C],
			},
			["voices"] = voices,
		};
	}

	public static JsonNode ResetEmulator(JsonNode? args)
	{
		// Power-cycle (full reset) is what tests usually want — equivalent to
		// flipping the cart off and back on. Soft Reset preserves more
		// state. Default to PowerCycle so frame counter actually rewinds.
		bool power = args?["power"]?.GetValue<bool>() ?? true;
		if(power) {
			EmuApi.McpPowerCycle();
		} else {
			EmuApi.McpResetEmu();
		}
		// Reset is processed on the emu thread; give it a beat to land
		// before the caller queries state.
		for(int i = 0; i < 60; i++) {
			System.Threading.Thread.Sleep(20);
			if(EmuApi.GetFrameCount() < 30) break;  // counter rewound
		}
		return new JsonObject { ["reset"] = true, ["power"] = power };
	}

	public static JsonNode ReadDmaState(JsonNode? args)
	{
		// DMA registers live at $4300..$437F (8 channels × 16 bytes).
		// They're CPU-bus-readable in debug mode. Most fields are valid
		// for both DMA and HDMA; the active mode + interpretation
		// depends on the channel control register bits.
		var channels = new JsonArray();
		for(int ch = 0; ch < 8; ch++) {
			uint baseAddr = (uint)(0x4300 + ch * 0x10);
			byte[] regs = DebugApi.GetMemoryValues(MemoryType.SnesMemory, baseAddr, baseAddr + 10);
			channels.Add(new JsonObject {
				["channel"] = ch,
				["control"] = regs[0],
				["bbus"] = regs[1],
				["aBusAddrLo"] = regs[2],
				["aBusAddrMid"] = regs[3],
				["aBusBank"] = regs[4],
				["countLo"] = regs[5],
				["countHi"] = regs[6],
				["indirectBank"] = regs[7],
				["tableAddrLo"] = regs[8],
				["tableAddrHi"] = regs[9],
				["lineCounter"] = regs[10],
				// Convenience: 24-bit A-bus source as a single number.
				["aBusAddr"] = (uint)(regs[2] | (regs[3] << 8) | (regs[4] << 16)),
				["tableAddr"] = (ushort)(regs[8] | (regs[9] << 8)),
				["count"] = (ushort)(regs[5] | (regs[6] << 8)),
				["targetReg"] = $"$21{regs[1]:X2}",
			});
		}
		return new JsonObject { ["channels"] = channels };
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

	public static JsonNode AddExecHook(JsonNode? args) => AddHookImpl(args, McpHookKind.Exec, "add_exec_hook");
	public static JsonNode AddReadHook(JsonNode? args) => AddHookImpl(args, McpHookKind.Read, "add_read_hook");
	public static JsonNode AddWriteHook(JsonNode? args) => AddHookImpl(args, McpHookKind.Write, "add_write_hook");

	private static JsonNode AddHookImpl(JsonNode? args, McpHookKind kind, string toolName)
	{
		if(args == null) throw new McpException(-32602, $"{toolName} requires arguments");
		uint addr = RequireUInt(args, "address");
		uint endAddr = (uint?)args["endAddress"]?.GetValue<long>() ?? addr;
		string cpuStr = args["cpuType"]?.GetValue<string>() ?? "Snes";
		if(!Enum.TryParse<CpuType>(cpuStr, ignoreCase: true, out var cpu)) {
			throw new McpException(-32602, "unknown cpuType: " + cpuStr);
		}
		if(endAddr < addr) {
			throw new McpException(-32602, "endAddress must be >= address");
		}
		uint matchValue = (uint)((args["matchValue"]?.GetValue<long>()) ?? 0);
		uint matchValueMask = (uint)((args["matchValueMask"]?.GetValue<long>()) ?? 0);
		int handle = DebugApi.McpAddHook((byte)kind, cpu, addr, endAddr, matchValue, matchValueMask);
		return new JsonObject {
			["handle"] = handle,
			["kind"] = kind.ToString(),
			["cpuType"] = cpu.ToString(),
			["address"] = addr,
			["endAddress"] = endAddr,
			["matchValue"] = matchValue,
			["matchValueMask"] = matchValueMask,
		};
	}

	public static JsonNode RemoveHook(JsonNode? args)
	{
		if(args == null) throw new McpException(-32602, "remove_hook requires arguments");
		int handle = (int)RequireUInt(args, "handle");
		bool ok = DebugApi.McpRemoveHook(handle);
		return new JsonObject {
			["handle"] = handle,
			["removed"] = ok,
		};
	}

	// Sym-file cache: filename -> (mtime, list of (name, addr)). Reparse if
	// the file's mtime changes — typical workflow has the user rebuilding
	// the ROM frequently and addresses shift each time.
	private static readonly Dictionary<string, (DateTime mtime, List<(string name, uint addr)> rows)> _symCache
		= new(StringComparer.OrdinalIgnoreCase);
	private static readonly object _symCacheLock = new();

	private static List<(string name, uint addr)> LoadSymFile(string path)
	{
		var info = new System.IO.FileInfo(path);
		if(!info.Exists) {
			throw new McpException(-32602, "sym file not found: " + path);
		}
		lock(_symCacheLock) {
			if(_symCache.TryGetValue(path, out var cached) && cached.mtime == info.LastWriteTimeUtc) {
				return cached.rows;
			}
			// WLA-DX format: "BBBB:OOOOOO NAME". Bank may be 4 hex digits, offset
			// is variable width. WRAM symbols encode the $7E bank in offset.
			var rows = new List<(string name, uint addr)>();
			var rx = new System.Text.RegularExpressions.Regex(
				@"^\s*[0-9A-Fa-f]{4}:([0-9A-Fa-f]+)\s+(\S+)\s*$");
			foreach(string line in System.IO.File.ReadAllLines(path)) {
				var m = rx.Match(line);
				if(!m.Success) continue;
				if(uint.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber,
						null, out uint addr)) {
					rows.Add((m.Groups[2].Value, addr));
				}
			}
			_symCache[path] = (info.LastWriteTimeUtc, rows);
			return rows;
		}
	}

	public static JsonNode LookupSymbol(JsonNode? args)
	{
		if(args == null) throw new McpException(-32602, "lookup_symbol requires arguments");
		string path = RequireString(args, "symFile");
		string pattern = RequireString(args, "pattern");
		int maxResults = (int)((args["maxResults"]?.GetValue<long>()) ?? 64);
		System.Text.RegularExpressions.Regex rx;
		try {
			rx = new System.Text.RegularExpressions.Regex(pattern);
		} catch(Exception ex) {
			throw new McpException(-32602, "invalid regex: " + ex.Message);
		}
		var rows = LoadSymFile(path);
		var matches = new JsonArray();
		int count = 0;
		foreach(var (name, addr) in rows) {
			if(!rx.IsMatch(name)) continue;
			// Common HiROM convenience: project's bank 0 ROM symbols (offset
			// 0x0000..0xFFFF) run at $C0:offset at runtime. WRAM symbols
			// already encode their full $7E:xxxx address in the offset and
			// must not be remapped.
			uint romCpu;
			if(addr < 0x10000) {
				romCpu = 0xC00000u | (addr & 0xFFFFu);
			} else {
				romCpu = addr;
			}
			matches.Add(new JsonObject {
				["name"] = name,
				["address"] = addr,
				["romCpuAddr"] = romCpu,
			});
			if(++count >= maxResults) break;
		}
		return new JsonObject {
			["count"] = count,
			["totalSymbols"] = rows.Count,
			["matches"] = matches,
		};
	}

	public static JsonNode Disassemble(JsonNode? args)
	{
		if(args == null) throw new McpException(-32602, "disassemble requires arguments");
		uint addr = RequireUInt(args, "address");
		int count = (int)((args["count"]?.GetValue<long>()) ?? 16);
		if(count <= 0 || count > 256) throw new McpException(-32602, "count out of range (1..256)");
		string cpuStr = args["cpuType"]?.GetValue<string>() ?? "Snes";
		if(!Enum.TryParse<CpuType>(cpuStr, ignoreCase: true, out var cpu)) {
			throw new McpException(-32602, "unknown cpuType: " + cpuStr);
		}

		// Get the row index for our address, then pull `count` consecutive rows.
		int startRow = DebugApi.GetDisassemblyRowAddress(cpu, addr, 0);
		CodeLineData[] rows = DebugApi.GetDisassemblyOutput(cpu, (uint)startRow, (uint)count);

		var lines = new JsonArray();
		foreach(var r in rows) {
			lines.Add(new JsonObject {
				["address"] = r.Address,
				["text"] = r.Text,
				["byteCode"] = Convert.ToHexString(r.ByteCode, 0, Math.Min(r.OpSize, r.ByteCode.Length)),
				["opSize"] = r.OpSize,
			});
		}
		return new JsonObject {
			["count"] = rows.Length,
			["lines"] = lines,
		};
	}

	public static JsonNode RunUntil(JsonNode? args)
	{
		args ??= new JsonObject();
		int maxFrames = (int)((args["maxFrames"]?.GetValue<long>()) ?? 600);
		int hookHandle = (int)((args["hookHandle"]?.GetValue<long>()) ?? 0);
		if(maxFrames <= 0 || maxFrames > 1_000_000) {
			throw new McpException(-32602, "maxFrames out of range (1..1000000)");
		}

		uint startFrame = EmuApi.GetFrameCount();
		ulong startMatches = 0;
		if(hookHandle != 0) {
			DebugApi.McpHookDiagCounters(out _, out startMatches);
		}

		EmuApi.Resume();
		string reason = "maxFrames";
		var sw = System.Diagnostics.Stopwatch.StartNew();
		// Poll loop: every ~10ms check (a) frame budget, (b) hook firing.
		// Cap real-time at 2x the frame-budget wall-clock just in case
		// emulation hangs.
		long maxMs = (long)(maxFrames * (1000.0 / 60.0) * 2.0 + 200);
		while(sw.ElapsedMilliseconds < maxMs) {
			System.Threading.Thread.Sleep(10);
			uint nowFrame = EmuApi.GetFrameCount();
			if(nowFrame - startFrame >= (uint)maxFrames) {
				reason = "maxFrames";
				break;
			}
			if(hookHandle != 0) {
				DebugApi.McpHookDiagCounters(out _, out ulong nowMatches);
				if(nowMatches > startMatches) {
					reason = "hookFired";
					break;
				}
			}
		}
		EmuApi.Pause();
		for(int i = 0; i < 200; i++) {
			if(EmuApi.IsPaused()) break;
			System.Threading.Thread.Sleep(5);
		}

		uint endFrame = EmuApi.GetFrameCount();
		return new JsonObject {
			["reason"] = reason,
			["framesAdvanced"] = endFrame - startFrame,
			["isPaused"] = EmuApi.IsPaused(),
		};
	}

	public static JsonNode HookDiag(JsonNode? args)
	{
		DebugApi.McpHookDiagCounters(out ulong calls, out ulong matches);
		return new JsonObject {
			["onMemoryOperationCalls"] = calls,
			["matchedEventsEmitted"] = matches,
		};
	}

	public static JsonNode ListHooks(JsonNode? args)
	{
		var buf = new McpHook[256];
		int n = DebugApi.McpListHooks(buf, buf.Length);
		var arr = new JsonArray();
		for(int i = 0; i < n; i++) {
			var h = buf[i];
			arr.Add(new JsonObject {
				["handle"] = h.Handle,
				["kind"] = h.Kind.ToString(),
				["cpuType"] = h.Cpu.ToString(),
				["startAddr"] = h.StartAddr,
				["endAddr"] = h.EndAddr,
				["active"] = h.Active,
			});
		}
		return new JsonObject {
			["count"] = n,
			["hooks"] = arr,
		};
	}

	public static JsonNode GetPpuState(JsonNode? args)
	{
		var s = DebugApi.GetPpuState<SnesPpuState>(CpuType.Snes);

		var layers = new JsonArray();
		for(int i = 0; i < s.Layers.Length; i++) {
			var L = s.Layers[i];
			layers.Add(new JsonObject {
				["index"] = i,
				["tilemapAddr"] = L.TilemapAddress,
				["chrAddr"] = L.ChrAddress,
				["hscroll"] = L.HScroll,
				["vscroll"] = L.VScroll,
				["largeTiles"] = L.LargeTiles,
				["doubleWidth"] = L.DoubleWidth,
				["doubleHeight"] = L.DoubleHeight,
			});
		}

		var mainMask = new JsonArray();
		var subMask = new JsonArray();
		for(int i = 0; i < s.WindowMaskMain.Length; i++) {
			mainMask.Add(JsonValue.Create((int)s.WindowMaskMain[i]));
			subMask.Add(JsonValue.Create((int)s.WindowMaskSub[i]));
		}

		return new JsonObject {
			["frameCount"] = s.FrameCount,
			["scanline"] = s.Scanline,
			["forcedBlank"] = s.ForcedBlank,
			["brightness"] = s.ScreenBrightness,
			["bgMode"] = s.BgMode,
			["mainScreenLayers"] = s.MainScreenLayers,  // bit0=BG1 .. bit4=OBJ
			["subScreenLayers"] = s.SubScreenLayers,
			["mainScreenWindowMask"] = mainMask,
			["subScreenWindowMask"] = subMask,
			["mosaicSize"] = s.MosaicSize,
			["mosaicEnabled"] = s.MosaicEnabled,
			["layers"] = layers,
		};
	}

	public static JsonNode SetInput(JsonNode? args)
	{
		if(args == null) {
			throw new McpException(-32602, "set_input requires arguments");
		}
		uint port = RequireUInt(args, "port");
		uint buttons = RequireUInt(args, "buttons");
		uint frames = RequireUInt(args, "frames");
		if(frames == 0 || frames > 100_000) {
			throw new McpException(-32602, "frames out of range (1..100000)");
		}
		if(port > 3) {
			throw new McpException(-32602, "port out of range (0..3)");
		}

		DebugControllerState state = new() {
			A = (buttons & 0x001) != 0,
			B = (buttons & 0x002) != 0,
			Select = (buttons & 0x004) != 0,
			Start = (buttons & 0x008) != 0,
			Up = (buttons & 0x010) != 0,
			Down = (buttons & 0x020) != 0,
			Left = (buttons & 0x040) != 0,
			Right = (buttons & 0x080) != 0,
			X = (buttons & 0x100) != 0,
			L = (buttons & 0x200) != 0,
			R = (buttons & 0x400) != 0,
			Y = (buttons & 0x800) != 0,
		};

		// Apply the override, run the emulator forward for the requested
		// frames, then clear the override. Debugger input overrides win
		// against the default controller polling every frame.
		DebugApi.SetInputOverrides(port, state);
		EmuApi.Resume();
		double estMs = frames * (1000.0 / 60.0);
		System.Threading.Thread.Sleep((int)Math.Min(estMs * 1.2 + 20, 120_000));
		EmuApi.Pause();
		for(int i = 0; i < 200; i++) {
			if(EmuApi.IsPaused()) break;
			System.Threading.Thread.Sleep(5);
		}
		DebugApi.SetInputOverrides(port, default);

		return new JsonObject {
			["port"] = port,
			["buttons"] = buttons,
			["frames"] = frames,
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
