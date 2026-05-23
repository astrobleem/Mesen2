#pragma once

#include <array>
#include <cstdint>
#include <optional>
#include <utility>
#include "Shared/CpuType.h"
#include "Shared/SettingTypes.h"

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
