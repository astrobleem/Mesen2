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
