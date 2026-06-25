# Mesen 2 — MCP runner mode

This fork adds a Model Context Protocol (MCP) server inside Mesen 2.
Run `Mesen.exe --mcp` and the emulator becomes a long-lived JSON-RPC
debugger that any MCP client (Claude Code, Cursor, your own scripts)
can drive over a TCP loopback socket.

The companion Python package `mesen_mcp` provides a stdio bridge and
typed client wrappers. The package and agent-onboarding docs live in
this repo under `python/` (`python/AGENTS.md`, `python/README.md`,
`python/CHANGELOG.md`).

## What you get

**47 MCP tools** organised into 10 categories. Highlights:

- **CPU/state**: live register snapshot (`get_cpu_state` — PC/A/X/Y/SP/D/DBR for the
  main `Snes` CPU *or* the `Sa1` coprocessor), plus PPU/audio/system state.
- **Memory**: read / write / multi-region diff over N frames.
- **Hooks**: exec / read / write / per-frame, with server-side value
  filters so high-volume PCs don't flood the wire.
- **PPU**: full BG tilemap render, tile sheet, OAM, CGRAM palette
  swatch grid, screenshot, multi-frame filmstrip.
- **Symbols**: WLA-DX `.sym` regex lookup, address-range to-nearest-
  symbol resolution, TheAnsarya/pansy v1.0 binary metadata reader.
- **Trace + disasm**: ring-buffer trace log, on-demand disassembly,
  watch-this-address-for-N-frames timeline.
- **Movies**: native `.mmo` record + playback wrapping Mesen's
  `RecordApi` for reproducible boot paths.
- **Audio**: WAV record, SHA-256 fingerprint + per-second RMS, PNG
  waveform renderer, S-DSP register snapshot.
- **Save states**: file path or numbered slot.

## Running

```bash
Mesen.exe --mcp [--mcp-port=N] path/to/rom.sfc
```

Logs land in `mcp_runner.log` and `mcp_server.log` under
`ConfigManager.HomeFolder` (e.g. `%APPDATA%\Mesen2\` on Windows).

The server listens on `127.0.0.1:7333` by default; pass
`--mcp-port=N` to override (or set deterministically per-cwd with the
Python bridge, which CRC's the working directory into [7350..7549]).

## Protocol

Newline-delimited JSON-RPC 2.0. One message per line. Standard MCP
methods (`initialize`, `tools/list`, `tools/call`, `shutdown`) plus
the asynchronous notification `notifications/mesen/hookFired` for
breakpoint-style events. See `Core/Mcp/McpHookManager.{h,cpp}` for the
C++ event queue (4096-event bounded deque, drained every 2ms by a
dedicated thread on the C# side).

## Build

If your native core is already current, the managed UI build is:

```bash
git clone https://github.com/astrobleem/Mesen2
cd Mesen2
dotnet build UI/UI.csproj -c Release
```

The `--mcp` mode is gated only by command-line argument parsing in
`UI/Utilities/Mcp/McpRunner.cs` - no extra feature flags are needed.

On Windows, a fresh checkout needs both halves of the build:

```powershell
msbuild Mesen.sln /p:Configuration=Release /p:Platform=x64 /m
dotnet build UI/UI.csproj -c Release -p:SolutionDir="$PWD\"
```

The first command builds the native `MesenCore.dll` with MCP hook exports;
the second builds the managed UI. If you build `UI/UI.csproj` directly,
pass `SolutionDir` explicitly. Otherwise MSBuild may resolve
`$(SolutionDir)` to a drive-level path such as `E:\bin\...` instead of the
repo-local `bin\win-x64\Release\` folder.

Point `MESEN_EXE` at the directory that contains a matched set of
`Mesen.exe`, `Mesen.dll`, and `MesenCore.dll`. A stale publish folder can
launch but never open the MCP socket if its managed UI or native core was
built before the MCP changes.

## Source layout

| Path | Purpose |
|---|---|
| `UI/Utilities/Mcp/McpServer.cs` | JSON-RPC dispatcher; tool table; per-client connection loop. |
| `UI/Utilities/Mcp/McpRunner.cs` | `--mcp` CLI bootstrap. Hooks into `Program.cs`. |
| `UI/Utilities/Mcp/McpTools.cs` | All 47 tool implementations + their schemas. |
| `UI/Utilities/Mcp/PansyReader.cs` | Standalone TheAnsarya/pansy v1.0 binary reader. |
| `Core/Mcp/McpHookManager.{h,cpp}` | C++ side of the hook hot-path. Bounded event queue. |
| `Core/SNES/SnesMemoryManager.cpp` | (touched) — SA-1 `Peek` fix routing $7E/$7F to BWRAM properly. |
| `InteropDLL/DebugApiWrapper.cpp` | P/Invoke surface for `McpDrainEvents` / `McpResetHooks`. |

## Adding a new tool

The pattern is in `McpTools.cs`:

1. Append a `new McpToolDesc(name, description, schema)` to
   `Descriptions` (line 21+).
2. Add a `BuildXxxSchema()` JsonNode builder (or pass `null` for
   no-arg tools).
3. Implement a `public static JsonNode Xxx(JsonNode? args)` handler.
4. Wire `["xxx"] = McpTools.Xxx` into `BuildToolTable` in
   `McpServer.cs`.
5. Optionally add a typed wrapper in the Python `McpSession`.

Tool descriptions are read by clients via `tools/list`; agents see
them verbatim, so make them informative.

## Upstream relationship

This is a fork of [`SourMesen/Mesen2`](https://github.com/SourMesen/Mesen2).
The MCP layer is intentionally kept in the fork (separate from the
core debugger UI). Pieces that are clearly upstream-eligible (e.g.
the SA-1 `Peek` fix, isolated debugger primitives) may be submitted
as separate PRs against SourMesen/Mesen2 over time.

## License

GPL-3.0-or-later, inherited from upstream Mesen 2.
