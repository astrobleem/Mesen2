# MCP → Nexen port — scope & plan

Move the 47-tool MCP server from this fork (`astrobleem/Mesen2`, based on the older Mesen2) onto
**`TheAnsarya/Nexen`** (a far more advanced, actively-developed fork based on `SourNexen/Nexen2`),
then offer it upstream as a PR.

## Why

- **We gain Nexen's RE stack** — 🌼 Pansy metadata *export* (we only read it today), a
  `LightweightCdlRecorder` (a second CDL source for our G1 coverage), an integrated SNES
  assembler + coprocessor disassemblers (Gsu/Cx4/NecDsp/Spc/St018), a TAS editor, ZIP movie
  format, and a real test/benchmark suite.
- **Nexen gains an MCP** — it has **none**. Andy already merged our SuperFX-HiROM PR; an
  agent-driving MCP is a natural, welcome contribution to the Flower Toolchain ecosystem.
- **We stop maintaining a stale base.** Our Mesen2 fork is old; Nexen is the living lineage.

## Tractability (verified, not assumed)

- All drop-in points exist in Nexen: `UI/Utilities/`, `UI/Program.cs`, `UI/Interop/DebugApi.cs` +
  `EmuApi.cs`, `InteropDLL/EmuApiWrapper.cpp` + `DebugApiWrapper.cpp`, `Core/Debugger/Debugger.*`.
- **Every STOCK managed API our MCP calls is present in Nexen with the same name** (`GetMemoryValues`,
  `GetPpuState`, `GetPaletteInfo`, `GetPpuToolsState`, `GetMemoryState`, `GetFrameCount`,
  `TakeScreenshot`, `SaveStateFile`/`LoadStateFile`, `RegisterNotificationCallback`, `SetInputOverrides`,
  `SetTraceOptions`, …). So the majority of the 47 tools port ~1:1.
- The **only custom engine code** is our hook system (the `Mcp*`-prefixed APIs); everything else is stock.

## Port surface

**Managed (C#)** — copy + reconcile:
- `UI/Utilities/Mcp/` — `McpServer.cs`, `McpTools.cs` (~112 KB, the bulk), `McpRunner.cs`, `PansyReader.cs` (~4k LOC).
- `UI/Program.cs` — the `--mcp` CLI hook.
- `UI/Interop/DebugApi.cs` + `EmuApi.cs` — P/Invoke decls for the custom `Mcp*` exports (stock decls already exist in Nexen).

**Native (C++)** — the real work (custom subsystem):
- `Core/Mcp/McpHookManager.{cpp,h}` — our exec/read/write/frame hook manager (a new Core dir).
- `Core/Debugger/Debugger.{cpp,h}` — dispatch edits that call into McpHookManager.
- `InteropDLL/EmuApiWrapper.cpp` + `DebugApiWrapper.cpp` — C exports: `McpAddHook`, `McpHookDiagCounters`,
  `McpResetEmu`, `McpPowerCycle` (+ the hook-notification plumbing).
- `Core/Core.vcxproj` (and the Linux makefile/cmake) — build wiring for `Core/Mcp/`.

**Python** — minimal:
- `python/mesen_mcp/` is protocol-only (JSON-RPC over the `--mcp` socket). Changes: the exe
  name/path (Mesen→Nexen), env var names if rebranded. Otherwise drop-in.

## Phased plan (mirrors how the MCP was built → de-risks incrementally)

The MCP went in as 4 commits — reuse that ordering:
1. `c48cd1f1` `--mcp` headless TCP JSON-RPC server skeleton
2. `5268f8a4` exec hooks + notification push
3. `78e0568d` read/write hooks, value-match, lookup_symbol, disassemble, run_until, frame counter
4. `5eb0cb07` crop/screenshot, save slots, DMA, frame hooks, reset (+ later `get_cpu_state`, Pansy)

**Phase 0 — baseline.** Fork `TheAnsarya/Nexen` → `astrobleem/Nexen`; branch; build stock Nexen
on Linux (vcpkg + .NET 10; note the AOT/SDL2 specifics) to establish a green baseline.

**Phase 1 — server + stock-only tools.** Port the `--mcp` server skeleton (commit 1) and only the
tools that hit stock APIs (ping, get_state, get_cpu_state, get_ppu_state, pause/resume, run_frames,
read/write_memory, memory_diff, screenshots, save/load state+slots, palette/tilemap/oam/tilesheet
renders, set_input, disassemble, lookup_symbol/pansy, audio, movies). **No native hook system yet.**
Validate: server starts; `ping`/`read_memory`/`run_frames`/`take_screenshot` work. This proves the
build + the stock managed-API mapping — the cheap 80% of the surface.

**Phase 2 — the hook system (the risk).** Port `Core/Mcp/McpHookManager` + the `Debugger.cpp`
dispatch + the `InteropDLL` exports + the P/Invoke decls. **First investigate** whether Nexen's
debugger already exposes a hook/breakpoint/memory-callback primitive to build on (its CDL recorder
+ execution-trace work suggest richer infra than our base) — prefer building on it over transplanting
ours. Enables: `add_exec/read/write/frame_hook`, `run_until`, `remove_hook`, `list_hooks`, `hook_diag`.

**Phase 3 — reconcile with Nexen's newer equivalents.** Where Nexen already does it better, use *its*
API (e.g. Pansy export, any improved PPU tools, CDL). Wire `lookup_pansy` to Nexen's Pansy layer.

**Phase 4 — Python + docs + full validation.** Point `mesen_mcp` at the Nexen exe; update the
README.MCP.md / catalog. Run the `mesen_mcp` validation suite AND **re-run the Superman flyval
pipeline against Nexen-MCP** to prove behavioral parity — especially SA-1 `get_cpu_state`, BW-RAM
reads (`Sa1Memory`/`$40`/`$41`), and save-state slot round-trip (the things our lockstep harness
depends on).

**Phase 5 — PR(s) to `TheAnsarya/Nexen`.** Consider splitting: PR the server skeleton + stock tools
first (easy review), then the hook system. Co-develop with Andy via the new Discussions.

## Risks / open questions
- **Native hook integration** is the one hard part — Nexen's `Debugger` has diverged (CDL recorder,
  trace tooling). Resolve in Phase 2 by building on its primitives, not transplanting blindly.
- **Build system divergence** — Nexen uses vcpkg + AOT + a different Core build; the `Core/Mcp/`
  wiring and the InteropDLL exports must be added to *Nexen's* build, not ours.
- **Behavioral parity for our pipeline** — the Superman flyval lockstep assumes exact emulator
  behavior; Phase 4 must confirm SA-1 + BW-RAM + save-state behave identically (else our `ON-vs-OFF=0`
  validation shifts). This is the acceptance gate.
- **Rebrand drift** — Nexen renamed types/namespaces in places; expect mechanical `Mesen→Nexen` fixups.

## Don't disrupt Superman
Keep `astrobleem/Mesen2` as the working base for the Superman transpiler finish. Do the Nexen port
**in parallel / after** — it's a strategic infra investment, not a blocker. (Our current fork +
47-tool MCP already validates every escape.)

## Effort
Phase 1 is moderate (stock APIs map ~1:1). Phase 2 is the real cost (native Core + debugger). Phases
3–5 are mechanical + reconciliation. Plan ~4–6 focused sessions with build/validate cycles, not one.
