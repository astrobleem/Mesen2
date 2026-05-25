#include "pch.h"
#include "Debugger/DebugUtilities.h"

TEST(DebugUtilitiesTests, CpuTypeMetadataMatchesExpectedMemoryTypeAndProgramCounterWidth) {
	struct ExpectedCpuMetadata {
		CpuType Cpu;
		MemoryType Memory;
		int PcWidth;
	};

	static constexpr std::array<ExpectedCpuMetadata, 17> kExpected = {{
		{CpuType::Snes, MemoryType::SnesMemory, 6},
		{CpuType::Spc, MemoryType::SpcMemory, 4},
		{CpuType::NecDsp, MemoryType::NecDspMemory, 6},
		{CpuType::Sa1, MemoryType::Sa1Memory, 6},
		{CpuType::Gsu, MemoryType::GsuMemory, 6},
		{CpuType::Cx4, MemoryType::Cx4Memory, 6},
		{CpuType::St018, MemoryType::St018Memory, 8},
		{CpuType::Gameboy, MemoryType::GameboyMemory, 4},
		{CpuType::Nes, MemoryType::NesMemory, 4},
		{CpuType::Pce, MemoryType::PceMemory, 4},
		{CpuType::Sms, MemoryType::SmsMemory, 4},
		{CpuType::Gba, MemoryType::GbaMemory, 8},
		{CpuType::Ws, MemoryType::WsMemory, 5},
		{CpuType::Lynx, MemoryType::LynxMemory, 4},
		{CpuType::Genesis, MemoryType::GenesisMemory, 6},
		{CpuType::Atari2600, MemoryType::Atari2600Memory, 4},
		{CpuType::ChannelF, MemoryType::ChannelFMemory, 4}
	}};

	for (const ExpectedCpuMetadata& expected : kExpected) {
		EXPECT_EQ(DebugUtilities::GetCpuMemoryType(expected.Cpu), expected.Memory);
		EXPECT_EQ(DebugUtilities::GetProgramCounterSize(expected.Cpu), expected.PcWidth);
	}
}

TEST(DebugUtilitiesTests, CpuMemoryTypeRoundtripsBackToOwningCpuType) {
	static constexpr std::array<CpuType, 17> kCpuTypes = {{
		CpuType::Snes,
		CpuType::Spc,
		CpuType::NecDsp,
		CpuType::Sa1,
		CpuType::Gsu,
		CpuType::Cx4,
		CpuType::St018,
		CpuType::Gameboy,
		CpuType::Nes,
		CpuType::Pce,
		CpuType::Sms,
		CpuType::Gba,
		CpuType::Ws,
		CpuType::Lynx,
		CpuType::Genesis,
		CpuType::Atari2600,
		CpuType::ChannelF
	}};

	for (CpuType cpuType : kCpuTypes) {
		MemoryType memoryType = DebugUtilities::GetCpuMemoryType(cpuType);
		EXPECT_EQ(DebugUtilities::ToCpuType(memoryType), cpuType);
	}
}
