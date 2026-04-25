# Mesen-MCP

A fork of [Mesen2](https://github.com/SourMesen/Mesen2) with a **native Model Context Protocol server** built into the emulator. Same 9-system emulation core (NES, SNES, GB, GBA, PCE, SMS/GG, WS) — but launchable as a headless agent that an LLM, a test harness, or a CI job can drive over a TCP socket.

Where vanilla Mesen has a Lua scripting console, this fork adds:

- A **`--mcp`** launch mode: loads a ROM, brings the emulator to max speed, listens on `127.0.0.1:7333`.
- **30+ JSON-RPC tools** for memory I/O, screenshots, save states, audio capture, symbol lookup, disassembly, hook registration, and more.
- **Bidirectional notifications** so a hook on a CPU instruction or memory write is delivered to the client mid-frame, not polled.
- A **Python client library** (`mcp_client.py`) and a small SKILL document (`AGENTS.md`) that any LLM coding agent can read first to be productive in five minutes.

If you've ever cursed at `tools/run_mesen.bat | grep ...` for the tenth time, this is the fix.

## Quick start

```bash
# Build (Windows; CL/Visual Studio + .NET 8)
msbuild Mesen.sln /p:Configuration=Release /p:Platform=x64 /t:Core,InteropDLL
dotnet build UI/UI.csproj -c Release

# Launch headless on port 7333
Mesen.exe --mcp /path/to/your.sfc

# In another terminal, drive it from Python
python3 -c "
import socket, json
s = socket.create_connection(('127.0.0.1', 7333))
s.sendall(b'{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}\n')
print(s.recv(4096))
"
```

For a Python client with typed methods, copy [`mcp_client.py`](https://github.com/astrobleem/Mesen2/blob/master/AGENTS.md) and:

```python
from mcp_client import McpSession

with McpSession(rom='path/to/game.sfc') as m:
    m.run_frames(500)                                  # advance emulation
    m.add_write_hook(0x7E0123)                         # WRAM write watcher
    m.run_frames(60)
    for evt in m.drain_notifications():                # async events
        print(evt['params'])                           # {handle, address, value, frame}
    shot = m.take_screenshot()                         # writes a PNG
    state = m.get_audio_state()                        # SPC700 + DSP voices
    m.save_state_slot(0)                               # checkpoint
```

## What's in the protocol

JSON-RPC 2.0 over a loopback TCP socket, newline-delimited. Server-to-client notifications use `notifications/mesen/hookFired` (no `id`).

**Tools** (30):

| Group | Tools |
|---|---|
| Session | `initialize`, `shutdown`, `ping` |
| Control | `pause`, `resume`, `run_frames`, `run_until`, `reset_emulator` |
| State | `get_state`, `get_ppu_state`, `get_audio_state`, `read_dma_state`, `hook_diag` |
| Memory | `read_memory`, `write_memory` |
| Hooks (event push) | `add_exec_hook`, `add_read_hook`, `add_write_hook`, `add_frame_hook`, `remove_hook`, `list_hooks` |
| Symbols / disasm | `lookup_symbol` (WLA-DX `.sym`), `disassemble` |
| Screenshots | `take_screenshot`, `crop_screenshot` |
| Save states | `save_state`, `load_state`, `save_state_slot`, `load_state_slot` |
| Audio | `record_audio`, `stop_audio` |
| Input | `set_input` |

Hooks support an optional `matchValue` + `matchValueMask` so a watch on a hot address (every-instruction, every-memory-byte) can be filtered server-side and never floods the socket.

Full protocol notes for AI coding agents are in **[AGENTS.md](AGENTS.md)**.

## What this fork adds (vs upstream Mesen)

| Layer | File(s) | Purpose |
|---|---|---|
| Core/MCP server | `Core/Mcp/McpHookManager.{h,cpp}` | Thread-safe hook registry; emitter on emulator thread; bounded event queue. |
| Debugger hooks | `Core/Debugger/Debugger.{h,cpp}` | One extra `if(_mcpHooks->HasAnyHooks())` per memory hot path; cost is one atomic-bool load when no hooks are registered. |
| Stdout discipline | `Core/Debugger/Debugger.cpp`, `Core/Debugger/ScriptingContext.cpp` | New `EmulationFlags::McpMode` redirects debugger logs to stderr so MCP can own stdout/socket cleanly. |
| InteropDLL exports | `InteropDLL/DebugApiWrapper.cpp`, `InteropDLL/EmuApiWrapper.cpp` | C ABI surface: `McpAddHook`, `McpRemoveHook`, `McpListHooks`, `McpDrainEvents`, `McpResetHooks`, `McpHookDiagCounters`, `GetFrameCount`, `McpResetEmu`, `McpPowerCycle`. |
| MCP server (UI) | `UI/Utilities/Mcp/{McpServer,McpTools,McpRunner}.cs` | C# server: TCP accept loop, request dispatch, drain thread for notifications, all 30 tools. |
| CLI | `UI/Program.cs`, `UI/Utilities/CommandLineHelper.cs` | New `--mcp` and `--mcp-port=N` flags. |

The non-MCP code paths are unchanged. Vanilla Mesen workflows still work.

## Releases / pre-built binaries

This fork is currently source-only. Vanilla Mesen pre-builds are at the [upstream releases page](https://github.com/SourMesen/Mesen2/releases).

## Compiling

See **[COMPILING.md](COMPILING.md)**. The MCP layer adds no new dependencies — it uses `System.Text.Json` (in .NET 8) and SkiaSharp (already shipped with Mesen) for crop_screenshot. No JSON library needed in C++.

## Related projects

- **[crlsh/mesen-mcp](https://github.com/crlsh/mesen-mcp)** — earlier C++-only Mesen-MCP fork with 6 tools and no event push. We landed on a wider tool set + bidirectional notifications, but theirs is a clean reference for the typed-command-on-emulator-thread pattern.
- **[TheAnsarya/Nexen](https://github.com/TheAnsarya/Nexen)** — Mesen2 fork with TAS editor, ZIP movie format, and Pansy disassembly metadata. Worth reading if you need TAS-style automated input recording on top of MCP.

## License

This fork inherits Mesen's GPL v3.

Mesen is Copyright © 2014–2025 Sour.  Full text: <http://www.gnu.org/licenses/gpl-3.0.en.html>.

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
