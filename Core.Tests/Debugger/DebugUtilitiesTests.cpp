#include "pch.h"
#include "Debugger/DebugUtilities.h"

TEST(DebugUtilitiesTests, CpuTypeMetadataMatchesExpectedMemoryTypeAndProgramCounterWidth) {
	struct ExpectedCpuMetadata {
		CpuType Cpu;
		MemoryType Memory;
		MemoryType PrgRom;
		int PcWidth;
	};

	static constexpr std::array<ExpectedCpuMetadata, 17> kExpected = {{
		{CpuType::Snes, MemoryType::SnesMemory, MemoryType::SnesPrgRom, 6},
		{CpuType::Spc, MemoryType::SpcMemory, MemoryType::SpcRom, 4},
		{CpuType::NecDsp, MemoryType::NecDspMemory, MemoryType::DspProgramRom, 6},
		{CpuType::Sa1, MemoryType::Sa1Memory, MemoryType::SnesPrgRom, 6},
		{CpuType::Gsu, MemoryType::GsuMemory, MemoryType::SnesPrgRom, 6},
		{CpuType::Cx4, MemoryType::Cx4Memory, MemoryType::SnesPrgRom, 6},
		{CpuType::St018, MemoryType::St018Memory, MemoryType::St018PrgRom, 8},
		{CpuType::Gameboy, MemoryType::GameboyMemory, MemoryType::GbPrgRom, 4},
		{CpuType::Nes, MemoryType::NesMemory, MemoryType::NesPrgRom, 4},
		{CpuType::Pce, MemoryType::PceMemory, MemoryType::PcePrgRom, 4},
		{CpuType::Sms, MemoryType::SmsMemory, MemoryType::SmsPrgRom, 4},
		{CpuType::Gba, MemoryType::GbaMemory, MemoryType::GbaPrgRom, 8},
		{CpuType::Ws, MemoryType::WsMemory, MemoryType::WsPrgRom, 5},
		{CpuType::Lynx, MemoryType::LynxMemory, MemoryType::LynxPrgRom, 4},
		{CpuType::Genesis, MemoryType::GenesisMemory, MemoryType::GenesisPrgRom, 6},
		{CpuType::Atari2600, MemoryType::Atari2600Memory, MemoryType::Atari2600PrgRom, 4},
		{CpuType::ChannelF, MemoryType::ChannelFMemory, MemoryType::ChannelFCartRom, 4}
	}};

	for (const ExpectedCpuMetadata& expected : kExpected) {
		EXPECT_EQ(DebugUtilities::GetCpuMemoryType(expected.Cpu), expected.Memory);
		EXPECT_EQ(DebugUtilities::GetPrgRomMemoryType(expected.Cpu), expected.PrgRom);
		EXPECT_EQ(DebugUtilities::GetProgramCounterSize(expected.Cpu), expected.PcWidth);
		EXPECT_TRUE(DebugUtilities::IsRom(expected.PrgRom));
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
