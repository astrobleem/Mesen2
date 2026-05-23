#pragma once

#include <array>
#include <cstdint>
#include <optional>
#include <utility>
#include "Shared/CpuType.h"
#include "Shared/EventType.h"
#include "Shared/SettingTypes.h"
#include "Debugger/DebugTypes.h"

enum class CpuStateLayout : uint8_t {
	SnesCpu,
	Spc,
	NecDsp,
	Gsu,
	Cx4,
	ArmV3,
	GbCpu,
	NesCpu,
	PceCpu,
	SmsCpu,
	GbaCpu,
	WsCpu,
	LynxCpu,
	Atari2600Cpu,
	ChannelFCpu,
	GenesisM68k,
	Unknown
};

enum class PpuStateBackend : uint8_t {
	Snes,
	Gameboy,
	Nes,
	Pce,
	Sms,
	Gba,
	Ws,
	Lynx,
	Atari2600,
	ChannelF,
	None
};

[[nodiscard]] inline CpuStateLayout GetCpuStateLayout(CpuType cpuType) {
	switch (cpuType) {
		case CpuType::Snes:
		case CpuType::Sa1:
			return CpuStateLayout::SnesCpu;
		case CpuType::Spc:
			return CpuStateLayout::Spc;
		case CpuType::NecDsp:
			return CpuStateLayout::NecDsp;
		case CpuType::Gsu:
			return CpuStateLayout::Gsu;
		case CpuType::Cx4:
			return CpuStateLayout::Cx4;
		case CpuType::St018:
			return CpuStateLayout::ArmV3;
		case CpuType::Gameboy:
			return CpuStateLayout::GbCpu;
		case CpuType::Nes:
			return CpuStateLayout::NesCpu;
		case CpuType::Pce:
			return CpuStateLayout::PceCpu;
		case CpuType::Sms:
			return CpuStateLayout::SmsCpu;
		case CpuType::Gba:
			return CpuStateLayout::GbaCpu;
		case CpuType::Ws:
			return CpuStateLayout::WsCpu;
		case CpuType::Lynx:
			return CpuStateLayout::LynxCpu;
		case CpuType::Atari2600:
			return CpuStateLayout::Atari2600Cpu;
		case CpuType::ChannelF:
			return CpuStateLayout::ChannelFCpu;
		case CpuType::Genesis:
			return CpuStateLayout::GenesisM68k;
	}

	return CpuStateLayout::Unknown;
}

[[nodiscard]] inline std::optional<DebuggerFlags> GetDebuggerFlagForCpu(CpuType cpuType) {
	switch (cpuType) {
		case CpuType::Snes:
			return DebuggerFlags::SnesDebuggerEnabled;
		case CpuType::Spc:
			return DebuggerFlags::SpcDebuggerEnabled;
		case CpuType::NecDsp:
			return DebuggerFlags::NecDspDebuggerEnabled;
		case CpuType::Sa1:
			return DebuggerFlags::Sa1DebuggerEnabled;
		case CpuType::Gsu:
			return DebuggerFlags::GsuDebuggerEnabled;
		case CpuType::Cx4:
			return DebuggerFlags::Cx4DebuggerEnabled;
		case CpuType::St018:
			return DebuggerFlags::St018DebuggerEnabled;
		case CpuType::Gameboy:
			return DebuggerFlags::GbDebuggerEnabled;
		case CpuType::Nes:
			return DebuggerFlags::NesDebuggerEnabled;
		case CpuType::Pce:
			return DebuggerFlags::PceDebuggerEnabled;
		case CpuType::Sms:
			return DebuggerFlags::SmsDebuggerEnabled;
		case CpuType::Gba:
			return DebuggerFlags::GbaDebuggerEnabled;
		case CpuType::Ws:
			return DebuggerFlags::WsDebuggerEnabled;
		case CpuType::Lynx:
			return DebuggerFlags::LynxDebuggerEnabled;
		case CpuType::Atari2600:
			return DebuggerFlags::Atari2600DebuggerEnabled;
		case CpuType::ChannelF:
			return DebuggerFlags::ChannelFDebuggerEnabled;
		case CpuType::Genesis:
			break;
	}

	return std::nullopt;
}

[[nodiscard]] inline PpuStateBackend GetPpuStateBackendForCpu(CpuType cpuType) {
	switch (cpuType) {
		case CpuType::Snes:
		case CpuType::Spc:
		case CpuType::NecDsp:
		case CpuType::Sa1:
		case CpuType::Gsu:
		case CpuType::Cx4:
		case CpuType::St018:
			return PpuStateBackend::Snes;
		case CpuType::Gameboy:
			return PpuStateBackend::Gameboy;
		case CpuType::Nes:
			return PpuStateBackend::Nes;
		case CpuType::Pce:
			return PpuStateBackend::Pce;
		case CpuType::Sms:
			return PpuStateBackend::Sms;
		case CpuType::Gba:
			return PpuStateBackend::Gba;
		case CpuType::Ws:
			return PpuStateBackend::Ws;
		case CpuType::Lynx:
			return PpuStateBackend::Lynx;
		case CpuType::Atari2600:
			return PpuStateBackend::Atari2600;
		case CpuType::ChannelF:
			return PpuStateBackend::ChannelF;
		default:
			break;
	}

	return PpuStateBackend::None;
}

[[nodiscard]] inline CpuType ResolveEventCpuType(CpuType requestedCpuType, CpuType mainCpuType, bool hasRequestedCpuType) {
	return hasRequestedCpuType ? requestedCpuType : mainCpuType;
}

[[nodiscard]] inline bool ShouldFallbackToMainInputDebugger(bool hasRoutedInputDebugger, bool hasMainInputDebugger) {
	return !hasRoutedInputDebugger && hasMainInputDebugger;
}

struct ProcessEventDispatchContext {
	bool DebuggerOwnsInstance = false;
	bool HasRoutedInputDebugger = false;
	bool HasMainInputDebugger = false;
	bool DebuggerBlocked = false;
	bool HasRoutedEventManager = false;
};

struct ProcessEventDispatchOutcome {
	bool ShouldDispatchScriptEvent = false;
	std::optional<CpuType> InputDebuggerCpuType = std::nullopt;
	bool ShouldSendEventViewerRefresh = false;
	bool ShouldClearFrameEvents = false;
};

enum class SleepUntilResumeDecision : uint8_t {
	Continue,
	SkipForSuspendRequest,
	SkipForExecutionAlreadyStopped,
	SkipForBreakRequestMainCpuBoundary,
	SkipForForbiddenBreakpoint
};

struct SleepUntilResumeGuardContext {
	bool HasSuspendRequest = false;
	bool ExecutionAlreadyStopped = false;
	bool HasBreakRequest = false;
	bool SourceCpuIsMainCpu = false;
	bool AllowChangeProgramCounter = false;
	bool BreakpointForbidden = false;
};

[[nodiscard]] inline SleepUntilResumeDecision EvaluateSleepUntilResumeDecision(const SleepUntilResumeGuardContext& context) {
	if (context.HasSuspendRequest) {
		return SleepUntilResumeDecision::SkipForSuspendRequest;
	}

	if (context.ExecutionAlreadyStopped) {
		return SleepUntilResumeDecision::SkipForExecutionAlreadyStopped;
	}

	if (context.HasBreakRequest && (!context.SourceCpuIsMainCpu || !context.AllowChangeProgramCounter)) {
		return SleepUntilResumeDecision::SkipForBreakRequestMainCpuBoundary;
	}

	if (context.BreakpointForbidden) {
		return SleepUntilResumeDecision::SkipForForbiddenBreakpoint;
	}

	return SleepUntilResumeDecision::Continue;
}

[[nodiscard]] inline bool ShouldEmitSleepUntilResumeBreakNotification(BreakSource source, bool hasBreakRequest) {
	return source != BreakSource::Unspecified || !hasBreakRequest;
}

[[nodiscard]] inline int32_t GetSleepUntilResumeWaitDelayMs(bool hasBreakRequest) {
	return hasBreakRequest ? 1 : 10;
}

[[nodiscard]] inline bool ShouldDispatchScriptEvent(bool debuggerOwnsInstance) {
	return debuggerOwnsInstance;
}

[[nodiscard]] inline std::optional<CpuType> ResolveInputDebuggerCpuType(CpuType routedCpuType, CpuType mainCpuType, bool hasRoutedInputDebugger, bool hasMainInputDebugger) {
	if (hasRoutedInputDebugger) {
		return routedCpuType;
	}

	if (ShouldFallbackToMainInputDebugger(hasRoutedInputDebugger, hasMainInputDebugger)) {
		return mainCpuType;
	}

	return std::nullopt;
}

[[nodiscard]] inline ProcessEventDispatchOutcome ResolveProcessEventDispatchOutcome(EventType type, CpuType routedCpuType, CpuType mainCpuType, const ProcessEventDispatchContext& context) {
	ProcessEventDispatchOutcome outcome = {};
	outcome.ShouldDispatchScriptEvent = ShouldDispatchScriptEvent(context.DebuggerOwnsInstance);

	switch (type) {
		case EventType::InputPolled:
			outcome.InputDebuggerCpuType = ResolveInputDebuggerCpuType(routedCpuType, mainCpuType, context.HasRoutedInputDebugger, context.HasMainInputDebugger);
			break;
		case EventType::StartFrame:
			outcome.ShouldSendEventViewerRefresh = !context.DebuggerBlocked;
			outcome.ShouldClearFrameEvents = context.HasRoutedEventManager;
			break;
		default:
			break;
	}

	return outcome;
}

[[nodiscard]] inline int32_t GetPauseScanlineForCpu(CpuType cpuType) {
	static constexpr std::array<std::pair<CpuType, int32_t>, 10> kPauseScanlineByCpu = {{
		{CpuType::Snes, 240},
		{CpuType::Gameboy, 144},
		{CpuType::Nes, 241},
		{CpuType::Pce, 243},
		{CpuType::Sms, 240},
		{CpuType::Gba, 160},
		{CpuType::Ws, 145},
		{CpuType::Lynx, 102},
		{CpuType::Atari2600, 262},
		{CpuType::ChannelF, 64}
	}};

	for (const auto& entry : kPauseScanlineByCpu) {
		if (entry.first == cpuType) {
			return entry.second;
		}
	}

	return 0;
}

[[nodiscard]] inline bool IsBreakOptionEnabledForSource(BreakSource src, const DebugConfig& cfg) {
	switch (src) {
		case BreakSource::GbDisableLcdOutsideVblank:
			return cfg.GbBreakOnDisableLcdOutsideVblank;
		case BreakSource::GbInvalidVramAccess:
			return cfg.GbBreakOnInvalidVramAccess;
		case BreakSource::GbInvalidOamAccess:
			return cfg.GbBreakOnInvalidOamAccess;
		case BreakSource::NesBreakOnDecayedOamRead:
			return cfg.NesBreakOnDecayedOamRead;
		case BreakSource::NesBreakOnPpuScrollGlitch:
			return cfg.NesBreakOnPpuScrollGlitch;
		case BreakSource::NesBusConflict:
			return cfg.NesBreakOnBusConflict;
		case BreakSource::NesBreakOnCpuCrash:
			return cfg.NesBreakOnCpuCrash;
		case BreakSource::NesBreakOnExtOutputMode:
			return cfg.NesBreakOnExtOutputMode;
		case BreakSource::NesInvalidVramAccess:
			return cfg.NesBreakOnInvalidVramAccess;
		case BreakSource::NesInvalidOamWrite:
			return cfg.NesBreakOnInvalidOamWrite;
		case BreakSource::NesDmaInputRead:
			return cfg.NesBreakOnDmaInputRead;
		case BreakSource::PceBreakOnInvalidVramAddress:
			return cfg.PceBreakOnInvalidVramAddress;
		case BreakSource::GbaInvalidOpCode:
			return cfg.GbaBreakOnInvalidOpCode;
		case BreakSource::GbaUnalignedMemoryAccess:
			return cfg.GbaBreakOnUnalignedMemAccess;
		case BreakSource::SnesInvalidPpuAccess:
			return cfg.SnesBreakOnInvalidPpuAccess;
		case BreakSource::SnesReadDuringAutoJoy:
			return cfg.SnesBreakOnReadDuringAutoJoy;
		default:
			break;
	}

	return true;
}
