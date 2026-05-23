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
