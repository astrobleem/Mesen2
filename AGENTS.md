# Driving Mesen-MCP from a coding agent

Skim this once before you connect. It's the difference between "an LLM that uses
Mesen well" and "an LLM that fights the harness for an hour and gives up."

## What you're talking to

A patched build of [Mesen2](https://github.com/SourMesen/Mesen2) launched as:

```
Mesen.exe --mcp [--mcp-port=N] /path/to/rom.sfc
```

It listens on `127.0.0.1:N` (default 7333), accepts **one client at a time**, and
speaks newline-delimited JSON-RPC 2.0. Server stdout is the MCP channel; debugger
log lines go to stderr. You can also load `.spc` files for SNES audio
debugging — pass the `.spc` as the ROM path and the SPC700 starts playing
immediately.

## Connecting

Quickest path is the bundled Python client:

```python
from mcp_client import McpSession  # tools/mcp_client.py in the SNES project

with McpSession(rom='game.sfc') as m:
    m.call('initialize', {})  # done by __enter__ already
    print(m.call('tools/list')['result']['tools'])
```

If you're rolling your own client: do `initialize` first, then `tools/list`,
then `tools/call`s. Reads from the socket can interleave with
`notifications/mesen/hookFired` push messages — your client must keep a queue
of those and not assume every line is a response to your last request.

## Mental model: pause then read

The single most common rookie mistake is treating Mesen like a static state.
**It runs at max speed by default.** Two `read_memory` calls 50ms apart see
totally different states. The fix:

```python
m.pause()
state_a = m.read_memory("snesMemory", 0x7E0000, 256)
state_b = m.read_memory("snesMemory", 0x7E0000, 256)  # same as state_a
m.resume()
```

Always pause before a multi-call inspection. Or compose with `run_until` /
`run_frames` if you need to advance and then inspect.

## Tool cheat sheet

```
SESSION
  initialize, shutdown, ping

CONTROL
  pause                                   pause emulation; reads stable now
  resume                                  back to max speed
  run_frames(count)                       advance ~count frames, then pause
  run_until(maxFrames=600, hookHandle=N)  resume until hook fires or budget
  reset_emulator(power=true)              full power-cycle (frame counter to 0)

STATE
  get_state                               isRunning, isPaused, frameCount
  get_ppu_state                           BG layers, scroll, window mask
  get_audio_state                         SPC700 + 8 voices' env/pitch/vol
  read_dma_state                          all 8 DMA/HDMA channels decoded
  hook_diag                               counters: how often hot path fired

MEMORY
  read_memory(memoryType, address, length)        hex-encoded bytes back
  write_memory(memoryType, address, hex)          hex-encoded bytes in

  memoryType: snesMemory  (CPU bus, $00:0000..$7F:FFFF)
              snesWorkRam (just $7E:0000..$7F:FFFF)
              snesVideoRam, snesCgRam, snesSpriteRam, snesPrgRom
              sa1Memory (SA-1 chip's view), sa1IRam (3KB on-chip)

HOOKS  (server pushes notifications/mesen/hookFired when these match)
  add_exec_hook(address, endAddress?, matchValue?, matchValueMask?)
  add_read_hook(...)        same shape, fires on memory read in range
  add_write_hook(...)       same shape, fires on memory write in range
  add_frame_hook(everyN=1)  fires once per video frame (or every Nth)
  remove_hook(handle)
  list_hooks

  matchValueMask=0 disables value match (fire on every hit).
  matchValueMask=0xFF + matchValue=33 means "fire only when value byte is 33".
  Use this aggressively on hot addresses; otherwise the socket floods.

SYMBOLS / DISASM
  lookup_symbol(symFile, pattern, maxResults?)    regex match WLA-DX .sym
  disassemble(address, count=16, cpuType="Snes")  N instructions from PC

SCREENSHOTS
  take_screenshot(format="path"|"base64")
  crop_screenshot(x, y, width, height, format="path"|"base64")

SAVE STATES
  save_state(path) / load_state(path)            file-backed
  save_state_slot(slot) / load_state_slot(slot)  numbered 0..9

AUDIO
  record_audio(path)        starts WAV capture; pair with stop_audio
  stop_audio
  get_audio_state           live SPC + DSP voice register snapshot

INPUT
  set_input(port, buttons, frames)
    bitmask: a=1, b=2, select=4, start=8, up=16, down=32, left=64, right=128,
             x=256, l=512, r=1024, y=2048
    Holds the input for `frames` emulator frames, then releases.
```

## How notifications arrive

When you register a hook, the server pushes JSON like:

```json
{"jsonrpc":"2.0","method":"notifications/mesen/hookFired","params":{
  "handle": 1,
  "kind": "Exec",
  "cpuType": "Snes",
  "address": 12845094,
  "value": 162,
  "frame": 188
}}
```

These are interleaved with your tool responses. The Python client's
`drain_notifications()` reads any pending ones into a list. If you roll your
own, distinguish by presence of `"id"` (response) vs `"method"` (notification).

## Common workflows

### "Did the bug fire on this frame?"

```python
h = m.add_exec_hook(0xC04E2A)  # addr of the function you're suspicious of
m.run_frames(500)
hits = [n for n in m.drain_notifications() if n['params']['handle'] == h]
print(f"function ran {len(hits)} times in 500 frames")
m.remove_hook(h)
```

### "What's the SPC700 voice doing right now?"

```python
m.pause()
s = m.get_audio_state()
for v in s['voices']:
    if v['envelope'] > 0:
        print(f"voice {v['voice']}: pitch={v['pitch']} env={v['envelope']}")
```

### "Compare the recorded music to a reference"

```python
m.record_audio('/tmp/now.wav')
m.run_frames(600)         # 10 seconds at 60fps; less wall-clock at max speed
m.stop_audio()
# offline:
# python3 audio_analyze.py /tmp/now.wav --ref /tmp/expected.wav
```

The analyzer prints per-band dB delta — that's the objective signal "is this
song right" without ears.

### "Reach a known game state fast"

Either:

1. **Save state checkpoint**: do an expensive boot once, `save_state_slot(0)`,
   then start every test with `load_state_slot(0)`.
2. **`set_input` chain**: drive the menus with explicit START presses
   from MCP rather than spamming raw `setInput` from Lua.

Save states are ~10× faster than re-driving from boot.

### "Diff state between two reproduction paths"

When something renders differently in two code paths, capture every probeable
state region in both and diff:

```python
def dump(m):
    m.pause()
    return {
        'palette':  m.read_memory('snesCgRam', 0, 512),
        'oam':      m.read_memory('snesSpriteRam', 0, 0x220),
        'bg1tiles': m.read_memory('snesVideoRam', 0x0000, 0x7000),
        'tilemap':  m.read_memory('snesVideoRam', 0x3000, 0x1000),
        'ppu':      m.get_ppu_state(),
        'dma':      m.read_dma_state(),
    }

clean   = dump(m_clean)
corrupt = dump(m_corrupt)
for k in clean:
    if isinstance(clean[k], bytes) and clean[k] != corrupt[k]:
        print(f"{k}: differs")
```

This pattern uncovered an HDMA + main-screen layer mismatch on the project
this fork was built for. With Lua scripts it took a half-day; with this
diff loop it took ten minutes.

## Common gotchas

- **Stderr will fill its pipe.** If you use `subprocess.PIPE` for stderr,
  drain it in a thread. Mesen's debug logger (`[CPU] Uninitialized memory
  read: ...`) emits hundreds of lines per second during the first ~12k
  frames. A full stderr pipe blocks Mesen's `Console.Error.WriteLine`
  inside the MCP handler, deadlocking the request loop. The provided
  `mcp_client.py` does this correctly; copy that pattern.

- **`run_frames(N)` is wall-clock approximate.** At max speed the emulator
  runs many more frames than the requested count; we sleep for the
  estimated 60Hz duration plus headroom. Use `get_state().frameCount`
  for ground truth and compose with `run_until` if you need a hard
  upper bound.

- **VRAM reads are reliable when paused.** They're sometimes
  flaky in SA-1 mode while running because of emulation thread races —
  always `pause` first if you care about exact bytes.

- **One client at a time.** The server accepts a new connection only
  after the previous client disconnects. Hooks are reset on disconnect
  so a crashed client doesn't leak watchpoints into the next session.

- **Bounded event queue (4096).** A hook on a too-broad range will
  drop oldest events rather than back-pressure the emulator. If you're
  seeing fewer notifications than you expected, narrow the address
  range or add a `matchValue` filter.

## Where to read source

- `Core/Mcp/McpHookManager.{h,cpp}` — the hot-path event manager.
- `UI/Utilities/Mcp/McpServer.cs` — TCP accept loop + JSON-RPC dispatch.
- `UI/Utilities/Mcp/McpTools.cs` — every tool's argument parsing + impl.
- `UI/Utilities/Mcp/McpRunner.cs` — `--mcp` entry point.

If you're adding a new tool, the diff is one entry in the `Descriptions`
list, one entry in `BuildToolTable()`, one method on `McpTools`. Most
existing tools are 5–30 lines.

## Limitations / explicit non-goals (for now)

- No movie record/playback yet. See [TheAnsarya/Nexen](https://github.com/TheAnsarya/Nexen)'s `.nexen-movie` format if you need this; it's a tracked task to add.
- Frame-stepping uses wall-clock, not greenzone-style per-frame savestates. Tracked.
- No interactive `step_into` / `step_over` like a native debugger UI. Use
  exec hooks + `run_until` instead.
- WLA-DX `.sym` only for `lookup_symbol` (no Pansy yet).
