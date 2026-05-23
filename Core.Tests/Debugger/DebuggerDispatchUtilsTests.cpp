#include "pch.h"
#include "Debugger/DebuggerDispatchUtils.h"

TEST(DebuggerDispatchUtilsTests, PauseScanlineMappingMatchesExpectedSystems) {
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Snes), 240);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Gameboy), 144);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Nes), 241);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Pce), 243);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Sms), 240);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Gba), 160);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Ws), 145);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Lynx), 102);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Atari2600), 262);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::ChannelF), 64);
}

TEST(DebuggerDispatchUtilsTests, PauseScanlineMappingReturnsZeroForUnsupportedCpuTypes) {
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Spc), 0);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::NecDsp), 0);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Sa1), 0);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Gsu), 0);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Cx4), 0);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::St018), 0);
	EXPECT_EQ(GetPauseScanlineForCpu(CpuType::Genesis), 0);
}

TEST(DebuggerDispatchUtilsTests, CpuStateLayoutMappingMatchesExpectedCpuFamilies) {
	EXPECT_EQ(GetCpuStateLayout(CpuType::Snes), CpuStateLayout::SnesCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Sa1), CpuStateLayout::SnesCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Spc), CpuStateLayout::Spc);
	EXPECT_EQ(GetCpuStateLayout(CpuType::NecDsp), CpuStateLayout::NecDsp);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Gsu), CpuStateLayout::Gsu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Cx4), CpuStateLayout::Cx4);
	EXPECT_EQ(GetCpuStateLayout(CpuType::St018), CpuStateLayout::ArmV3);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Gameboy), CpuStateLayout::GbCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Nes), CpuStateLayout::NesCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Pce), CpuStateLayout::PceCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Sms), CpuStateLayout::SmsCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Gba), CpuStateLayout::GbaCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Ws), CpuStateLayout::WsCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Lynx), CpuStateLayout::LynxCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Atari2600), CpuStateLayout::Atari2600Cpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::ChannelF), CpuStateLayout::ChannelFCpu);
	EXPECT_EQ(GetCpuStateLayout(CpuType::Genesis), CpuStateLayout::GenesisM68k);
}

TEST(DebuggerDispatchUtilsTests, DebuggerFlagMappingMatchesExpectedCpuTargets) {
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Snes), DebuggerFlags::SnesDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Spc), DebuggerFlags::SpcDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::NecDsp), DebuggerFlags::NecDspDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Sa1), DebuggerFlags::Sa1DebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Gsu), DebuggerFlags::GsuDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Cx4), DebuggerFlags::Cx4DebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::St018), DebuggerFlags::St018DebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Gameboy), DebuggerFlags::GbDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Nes), DebuggerFlags::NesDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Pce), DebuggerFlags::PceDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Sms), DebuggerFlags::SmsDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Gba), DebuggerFlags::GbaDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Ws), DebuggerFlags::WsDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Lynx), DebuggerFlags::LynxDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Atari2600), DebuggerFlags::Atari2600DebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::ChannelF), DebuggerFlags::ChannelFDebuggerEnabled);
	EXPECT_EQ(GetDebuggerFlagForCpu(CpuType::Genesis), std::nullopt);
}

TEST(DebuggerDispatchUtilsTests, BreakSourceMappingUsesExpectedConfigFlags) {
	DebugConfig cfg = {};
	cfg.GbBreakOnDisableLcdOutsideVblank = true;
	cfg.NesBreakOnBusConflict = true;
	cfg.GbaBreakOnUnalignedMemAccess = true;
	cfg.SnesBreakOnReadDuringAutoJoy = true;

	EXPECT_TRUE(IsBreakOptionEnabledForSource(BreakSource::GbDisableLcdOutsideVblank, cfg));
	EXPECT_TRUE(IsBreakOptionEnabledForSource(BreakSource::NesBusConflict, cfg));
	EXPECT_TRUE(IsBreakOptionEnabledForSource(BreakSource::GbaUnalignedMemoryAccess, cfg));
	EXPECT_TRUE(IsBreakOptionEnabledForSource(BreakSource::SnesReadDuringAutoJoy, cfg));

	EXPECT_FALSE(IsBreakOptionEnabledForSource(BreakSource::NesInvalidVramAccess, cfg));
}

TEST(DebuggerDispatchUtilsTests, BreakSourceMappingDefaultsToTrueForUnmappedSources) {
	DebugConfig cfg = {};

	EXPECT_TRUE(IsBreakOptionEnabledForSource(BreakSource::Breakpoint, cfg));
	EXPECT_TRUE(IsBreakOptionEnabledForSource(BreakSource::CpuStep, cfg));
	EXPECT_TRUE(IsBreakOptionEnabledForSource(BreakSource::BreakOnBrk, cfg));
}

TEST(DebuggerDispatchUtilsTests, PpuBackendMappingRoutesGroupedCpuTypesToExpectedBackends) {
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Snes), PpuStateBackend::Snes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Spc), PpuStateBackend::Snes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::NecDsp), PpuStateBackend::Snes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Sa1), PpuStateBackend::Snes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Gsu), PpuStateBackend::Snes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Cx4), PpuStateBackend::Snes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::St018), PpuStateBackend::Snes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Gameboy), PpuStateBackend::Gameboy);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Nes), PpuStateBackend::Nes);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Pce), PpuStateBackend::Pce);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Sms), PpuStateBackend::Sms);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Gba), PpuStateBackend::Gba);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Ws), PpuStateBackend::Ws);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Lynx), PpuStateBackend::Lynx);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Atari2600), PpuStateBackend::Atari2600);
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::ChannelF), PpuStateBackend::ChannelF);
}

TEST(DebuggerDispatchUtilsTests, PpuBackendMappingReturnsNoneForUnsupportedCpuTypes) {
	EXPECT_EQ(GetPpuStateBackendForCpu(CpuType::Genesis), PpuStateBackend::None);
}

TEST(DebuggerDispatchUtilsTests, EventCpuRoutingUsesRequestedCpuWhenAvailable) {
	EXPECT_EQ(ResolveEventCpuType(CpuType::Nes, CpuType::Snes, true), CpuType::Nes);
	EXPECT_EQ(ResolveEventCpuType(CpuType::Gba, CpuType::Snes, true), CpuType::Gba);
}

TEST(DebuggerDispatchUtilsTests, EventCpuRoutingFallsBackToMainCpuWhenRequestedCpuUnavailable) {
	EXPECT_EQ(ResolveEventCpuType(CpuType::Nes, CpuType::Snes, false), CpuType::Snes);
	EXPECT_EQ(ResolveEventCpuType(CpuType::ChannelF, CpuType::Gameboy, false), CpuType::Gameboy);
}

TEST(DebuggerDispatchUtilsTests, InputDebuggerFallbackDecisionRequiresMissingRoutedAndPresentMain) {
	EXPECT_TRUE(ShouldFallbackToMainInputDebugger(false, true));
	EXPECT_FALSE(ShouldFallbackToMainInputDebugger(true, true));
	EXPECT_FALSE(ShouldFallbackToMainInputDebugger(false, false));
}

TEST(DebuggerDispatchUtilsTests, ScriptDispatchRequiresDebuggerOwnership) {
	EXPECT_TRUE(ShouldDispatchScriptEvent(true));
	EXPECT_FALSE(ShouldDispatchScriptEvent(false));
}

TEST(DebuggerDispatchUtilsTests, InputDebuggerCpuResolutionPrefersRoutedThenMainThenNone) {
	EXPECT_EQ(ResolveInputDebuggerCpuType(CpuType::Nes, CpuType::Snes, true, true), CpuType::Nes);
	EXPECT_EQ(ResolveInputDebuggerCpuType(CpuType::Nes, CpuType::Snes, false, true), CpuType::Snes);
	EXPECT_FALSE(ResolveInputDebuggerCpuType(CpuType::Nes, CpuType::Snes, false, false).has_value());
}

TEST(DebuggerDispatchUtilsTests, ProcessEventDispatchOutcomeComposesInputFallbackAndScriptOwnership) {
	ProcessEventDispatchContext context = {};
	context.DebuggerOwnsInstance = false;
	context.HasRoutedInputDebugger = false;
	context.HasMainInputDebugger = true;

	ProcessEventDispatchOutcome outcome = ResolveProcessEventDispatchOutcome(EventType::InputPolled, CpuType::Nes, CpuType::Snes, context);
	EXPECT_FALSE(outcome.ShouldDispatchScriptEvent);
	ASSERT_TRUE(outcome.InputDebuggerCpuType.has_value());
	EXPECT_EQ(outcome.InputDebuggerCpuType.value(), CpuType::Snes);
	EXPECT_FALSE(outcome.ShouldSendEventViewerRefresh);
	EXPECT_FALSE(outcome.ShouldClearFrameEvents);
}

TEST(DebuggerDispatchUtilsTests, ProcessEventDispatchOutcomeComposesStartFrameRefreshAndEventManagerFlags) {
	ProcessEventDispatchContext activeContext = {};
	activeContext.DebuggerOwnsInstance = true;
	activeContext.DebuggerBlocked = false;
	activeContext.HasRoutedEventManager = true;

	ProcessEventDispatchOutcome activeOutcome = ResolveProcessEventDispatchOutcome(EventType::StartFrame, CpuType::Nes, CpuType::Snes, activeContext);
	EXPECT_TRUE(activeOutcome.ShouldDispatchScriptEvent);
	EXPECT_TRUE(activeOutcome.ShouldSendEventViewerRefresh);
	EXPECT_TRUE(activeOutcome.ShouldClearFrameEvents);

	ProcessEventDispatchContext blockedContext = {};
	blockedContext.DebuggerOwnsInstance = true;
	blockedContext.DebuggerBlocked = true;
	blockedContext.HasRoutedEventManager = false;

	ProcessEventDispatchOutcome blockedOutcome = ResolveProcessEventDispatchOutcome(EventType::StartFrame, CpuType::Nes, CpuType::Snes, blockedContext);
	EXPECT_TRUE(blockedOutcome.ShouldDispatchScriptEvent);
	EXPECT_FALSE(blockedOutcome.ShouldSendEventViewerRefresh);
	EXPECT_FALSE(blockedOutcome.ShouldClearFrameEvents);
}
