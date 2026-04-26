# Mesen-MCP

A fork of [Mesen2](https://github.com/SourMesen/Mesen2) with a **native Model Context Protocol server** built into the emulator. Same 9-system emulation core (NES, SNES, GB, GBA, PCE, SMS/GG, WS) — but launchable as a headless agent that an LLM, a test harness, or a CI job can drive over a TCP socket.

> **For agents and integrators:** the canonical reference is **[`README.MCP.md`](README.MCP.md)** in this repo (transport, tool surface, source layout, build) and **[`tools/mesen_mcp/AGENTS.md`](https://github.com/astrobleem/SNES-SuperMonkeyIsland/blob/master/tools/mesen_mcp/AGENTS.md)** in the companion Python package (recipes, pitfalls, env vars). Read those first.

Where vanilla Mesen has a Lua scripting console, this fork adds:

- A **`--mcp`** launch mode: loads a ROM, brings the emulator to max speed, listens on `127.0.0.1:7333`.
- **46 JSON-RPC tools** for memory I/O + diff, screenshots + filmstrip, save states, movie record/playback, audio capture + fingerprinting, palette + tilemap + sprite rendering, symbol lookup (WLA-DX `.sym` and TheAnsarya/pansy v1.0), disassembly, trace log, hooks (exec/read/write/frame) with notifications, and more.
- **Bidirectional notifications** so a hook on a CPU instruction or memory write is delivered to the client mid-frame, not polled.
- A **portable Python client library** (`mesen_mcp` package — pip-installable, env-var-configured) and an **agent-onboarding doc** (`AGENTS.md`) that any LLM coding agent can read first to be productive in five minutes.

If you've ever cursed at `tools/run_mesen.bat | grep ...` for the tenth time, this is the fix.

## Quick start

```bash
# 1. Build the emulator (Windows; CL/Visual Studio + .NET 8)
msbuild Mesen.sln /p:Configuration=Release /p:Platform=x64 /t:Core,InteropDLL
dotnet build UI/UI.csproj -c Release

# 2. Install the Python client (lives inside this repo)
pip install -e python/mesen_mcp

# 3. Tell it where Mesen + your ROM live
export MESEN_EXE=$PWD/UI/bin/Release/net8.0/Mesen.exe
export MESEN_ROM=/path/to/your.sfc

# 4. Drive it
python -c "
from mesen_mcp import McpSession
with McpSession.from_env() as m:
    m.run_frames(600)
    m.add_write_hook(0x7E0123)
    m.run_frames(60)
    print(m.take_screenshot()['path'])
"
```

The fork is **self-contained**. Everything you need — emulator source, MCP server, Python client, agent docs, examples — lives in this repo. Clone it, build it, install the client, set two env vars. No other repos required.

## 👟 "What are those?!"

Caught an LLM driving Mesen the old way? Send them here.

If an agent's doing any of these, it's wearing the wrong shoes:

- ❌ Spawning `Mesen.exe --testrunner` for every state inspection (one-shot Lua, cold boot per call, parses stdout)
- ❌ `time.sleep(2)` then "hopefully the emulator advanced 120 frames" (it didn't — max-speed mode raced past)
- ❌ Decoding screenshot PNGs to figure out which room the game is in
- ❌ Reading `.sym` files in Python and threading regex through every test script
- ❌ Polling `read_memory` in a tight loop to watch an address change
- ❌ Hand-rolling its own image-diff with PIL because there's no way to ask the emulator what changed
- ❌ Adding `print()` calls to the game ROM to find out what a script does

Then yes — **what are those?!** That's vanilla Mesen automation.

The same workflow with `mesen-mcp`:

```python
from mesen_mcp import McpSession
with McpSession.from_env() as m:
    m.run_frames(600)                              # frame-exact, no sleep
    m.add_write_hook(0x7E0123)                     # async event push, server-filtered
    m.run_frames(60)
    print(m.drain_notifications())                 # every write, with frame numbers
    print(m.symbolic_dump("game.sym", 0x7EF000, 64))  # what's at this WRAM range?
    print(m.memory_diff([{"memoryType":"snesWorkRam","address":0,"length":0x2000}], frames=60))
    print(m.audio_fingerprint("intro.wav"))        # SHA-256 + per-second RMS
```

Six lines vs. an afternoon. **`pip install -e python/` and tell your friends.**

## For coding agents

Copy-paste this prompt to give an agent (Claude Code, Cursor, etc.) on a project that wants to use these tools:

> I want to drive the [`astrobleem/Mesen2`](https://github.com/astrobleem/Mesen2) fork's MCP debugger from this project. Clone the repo, build `Mesen.exe` (`dotnet build UI/UI.csproj -c Release`), then `pip install -e <fork>/python/mesen_mcp`. Read `<fork>/python/mesen_mcp/AGENTS.md` for the tool surface (46 tools across state/memory/screenshot/savestate/movies/ppu/hooks/debugging/input/audio) and the first-call protocol. Wire up `<my-project>/.mcp.json` with a `mesen-inproc` server pointing at `mesen-mcp-bridge` (the console script the package installs), with `MESEN_EXE` and `MESEN_ROM` set in its `env` block. Then `mesen-mcp-tools` lists every tool with one-line summaries — use that for discovery before each session.

Useful entry points after install:

- `mesen-mcp-tools` — categorised tool listing (CLI). `--filter <substr>` / `--category <cat>` / `--names`.
- `mesen-mcp-bridge` — stdio↔TCP bridge. Wire this into `.mcp.json` for any MCP-aware client.
- `python -m mesen_mcp.examples.boot_and_screenshot` — runnable starter template.
- `python/mesen_mcp/AGENTS.md` — first-call protocol, recipes, pitfalls.
- `python/mesen_mcp/README.md` — install steps, full tool table, env vars.

## What's in the protocol

JSON-RPC 2.0 over a loopback TCP socket, newline-delimited. Server-to-client notifications use `notifications/mesen/hookFired` (no `id`).

**46 tools across 10 categories:**

| Category | Tools |
|---|---|
| Session       | `initialize`, `shutdown`, `ping` |
| State         | `get_state`, `pause`, `resume`, `run_frames` (frame-exact), `reset_emulator` |
| Memory        | `read_memory`, `write_memory`, `memory_diff`, `read_dma_state` |
| Screenshot    | `take_screenshot`, `crop_screenshot`, `render_filmstrip` |
| Save states   | `save_state`, `load_state`, `save_state_slot`, `load_state_slot` |
| Movies        | `record_movie`, `play_movie`, `stop_movie`, `movie_state` |
| PPU/graphics  | `get_ppu_state`, `render_tilemap`, `render_tile_sheet`, `render_oam`, `render_palette` |
| Hooks         | `add_exec_hook`, `add_read_hook`, `add_write_hook`, `add_frame_hook`, `remove_hook`, `list_hooks`, `hook_diag`, `run_until` |
| Debugging     | `lookup_symbol` (WLA-DX `.sym`), `symbolic_dump`, `lookup_pansy` (TheAnsarya/pansy v1.0), `disassemble`, `trace_log`, `watch_addresses` |
| Input         | `set_input` |
| Audio         | `record_audio`, `stop_audio`, `get_audio_state`, `audio_fingerprint`, `audio_waveform_png` |

Hooks support an optional `matchValue` + `matchValueMask` so a watch on a hot address (every-instruction, every-memory-byte) can be filtered server-side and never floods the socket.

Full protocol + tool reference + agent-onboarding guide is in **[`python/mesen_mcp/AGENTS.md`](python/mesen_mcp/AGENTS.md)** and **[`README.MCP.md`](README.MCP.md)**.

## What this fork adds (vs upstream Mesen)

| Layer | File(s) | Purpose |
|---|---|---|
| Core/MCP server     | `Core/Mcp/McpHookManager.{h,cpp}` | Thread-safe hook registry; emitter on emulator thread; bounded event queue (4096 events). |
| Core/SA-1 fix       | `Core/SNES/SnesMemoryManager.cpp` | `Peek/PeekWord/PeekBlock` short-circuit `$7E/$7F` to direct WRAM, bypassing the SA-1 mapping hole. |
| Debugger hooks      | `Core/Debugger/Debugger.{h,cpp}` | One extra `if(_mcpHooks->HasAnyHooks())` per memory hot path; cost is one atomic-bool load when no hooks are registered. |
| Stdout discipline   | `Core/Debugger/Debugger.cpp`, `Core/Debugger/ScriptingContext.cpp` | New `EmulationFlags::McpMode` redirects debugger logs to stderr so MCP can own stdout/socket cleanly. |
| InteropDLL exports  | `InteropDLL/DebugApiWrapper.cpp`, `InteropDLL/EmuApiWrapper.cpp` | C ABI surface: `McpAddHook`, `McpRemoveHook`, `McpListHooks`, `McpDrainEvents`, `McpResetHooks`, `McpHookDiagCounters`, `GetFrameCount`, `McpResetEmu`, `McpPowerCycle`. |
| MCP server (UI)     | `UI/Utilities/Mcp/{McpServer,McpTools,McpRunner,PansyReader}.cs` | C# server: TCP accept loop, request dispatch, drain thread for notifications, all 46 tools, Pansy v1.0 metadata reader. |
| CLI                 | `UI/Program.cs`, `UI/Utilities/CommandLineHelper.cs` | New `--mcp` and `--mcp-port=N` flags. |
| Python client       | `python/mesen_mcp/` | Stdio↔TCP bridge + typed `McpSession` client + agent docs + 3 examples + tool catalog. Pip-installable. |

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
