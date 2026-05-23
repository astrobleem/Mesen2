#pragma once

#include <array>
#include <cstdint>
#include <utility>
#include "Shared/CpuType.h"

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
