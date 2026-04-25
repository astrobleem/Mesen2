#pragma once
#include "pch.h"
#include "Debugger/DebugTypes.h"
#include "Utilities/SimpleLock.h"
#include <atomic>
#include <deque>
#include <mutex>
#include <vector>

// Hook kinds. Kept simple — phase-3 MVP is exec-on-CPU only; read/write/
// scanline/frame can slot in as new HookKind values without churn.
enum class McpHookKind : uint8_t
{
	Exec = 0,
};

struct McpHook
{
	int32_t Handle;
	McpHookKind Kind;
	CpuType Cpu;
	uint32_t StartAddr;
	uint32_t EndAddr;
	bool Active;
};

// Event emitted when a hook fires. Kept POD + fixed-size so the C# side
// can marshal a contiguous array in one P/Invoke.
struct McpHookEvent
{
	int32_t Handle;
	uint32_t Address;
	uint32_t Value;       // 0 for exec hooks
	uint32_t FrameNumber;
	uint8_t Kind;         // McpHookKind
	uint8_t Cpu;          // CpuType
	uint8_t Padding[2];
};

// Thread-safe registry + bounded event queue. Hot path runs on the
// emulator thread (called from Debugger::ProcessMemoryRead/Write/Instruction
// via ProcessMemoryOperation). Drain runs on the MCP server thread.
//
// Design:
// - RegisterHook/UnregisterHook hold a mutex. Modifications are rare.
// - HasAnyHooks() is a cheap atomic read the hot path can use BEFORE it
//   even considers taking the mutex (same trick as ScriptManager).
// - OnMemoryOperation takes the mutex only if HasAnyHooks() is true.
// - Events enqueue under the same mutex. Bounded queue drops oldest if
//   full — a disconnected or slow client should not pressure the
//   emulator thread or consume unbounded memory.
class McpHookManager
{
public:
	static constexpr size_t MaxQueuedEvents = 4096;

	McpHookManager();

	// Registry.
	int32_t RegisterExecHook(CpuType cpu, uint32_t startAddr, uint32_t endAddr);
	bool UnregisterHook(int32_t handle);
	size_t CopyActiveHooks(McpHook* out, size_t maxCount);

	// Hot-path fast check. emulator thread.
	bool HasAnyHooks() const { return _hasAny.load(std::memory_order_relaxed); }

	// Diagnostic: total matches since last reset, and total OnMemoryOperation
	// invocations (regardless of match). Lets a client confirm the hot-path
	// integration is alive even if no hook address range matches.
	uint64_t GetCallCount() const { return _callCount.load(std::memory_order_relaxed); }
	uint64_t GetMatchCount() const { return _matchCount.load(std::memory_order_relaxed); }

	// Fire from emulator thread. Synchronously matches the op against all
	// registered hooks and pushes one event per match. Drops oldest events
	// if queue is full.
	void OnMemoryOperation(CpuType cpu, uint32_t addr, uint32_t value,
		McpHookKind kind, uint32_t frameNumber);

	// Drain events into out buffer. Returns number of events copied.
	// Emulator thread continues enqueueing concurrently; draining is O(n).
	size_t DrainEvents(McpHookEvent* out, size_t maxCount);

	// Clear all hooks + events. Called on MCP client disconnect so a crashed
	// client does not leak hooks into the running emulator.
	void Reset();

private:
	void UpdateHasAnyFlag();

	std::mutex _mutex;
	std::vector<McpHook> _hooks;
	std::deque<McpHookEvent> _events;
	std::atomic<bool> _hasAny{false};
	std::atomic<int32_t> _nextHandle{1};
	std::atomic<uint64_t> _callCount{0};
	std::atomic<uint64_t> _matchCount{0};
	size_t _droppedEvents = 0;
};
