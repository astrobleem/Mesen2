#pragma once
#include <array>
#include "pch.h"
#include "Debugger/DebugTypes.h"
#include "Shared/MemoryType.h"
#include "Utilities/HexUtilities.h"

/// <summary>
/// Static utility functions for debugger (CPU type conversion, memory classification).
/// </summary>
/// <remarks>
/// Architecture:
/// - Pure static class (no instantiation)
/// - Compile-time constant functions (constexpr)
/// - Platform-agnostic utilities
///
/// CPU type utilities:
/// - GetCpuMemoryType(): MemoryType → CpuType
/// - ToCpuType(): MemoryType → CpuType
/// - GetProgramCounterSize(): CpuType → address width
///
/// Memory classification:
/// - IsRelativeMemory(): Is CPU-addressable memory
/// - IsPpuMemory(): Is PPU memory (VRAM/OAM/palette)
/// - IsRom(): Is read-only memory
/// - IsVolatileRam(): Is volatile (not battery-backed)
///
/// Address formatting:
/// - AddressToHex(): Format address for CPU type (16/20/24/32-bit)
///
/// Platform support:
/// - NES, SNES (+ SA-1/SPC/GSU/etc), GB, GBA, PCE, SMS, WS
/// </remarks>
class DebugUtilities {
public:
	struct CpuTypeMetadata {
		CpuType Type;
		MemoryType CpuMemoryType;
		MemoryType PrgRomMemoryType;
		int ProgramCounterSize;
	};

	[[nodiscard]] static constexpr const CpuTypeMetadata& GetCpuTypeMetadata(CpuType type) {
		for (const CpuTypeMetadata& metadata : _cpuTypeMetadata) {
			if (metadata.Type == type) {
				return metadata;
			}
		}

		[[unlikely]] throw std::runtime_error("Invalid CPU type");
	}

	[[nodiscard]] static constexpr CpuType GetCpuTypeFromBaseMemoryType(MemoryType type) {
		for (const CpuTypeMetadata& metadata : _cpuTypeMetadata) {
			if (metadata.CpuMemoryType == type) {
				return metadata.Type;
			}
		}

		[[unlikely]] throw std::runtime_error("Invalid CPU memory type");
	}

	[[nodiscard]] static constexpr CpuType GetCpuTypeFromNonRelativeMemoryType(MemoryType type) {
		for (const MemoryTypeOwnerMapping& mapping : _nonRelativeMemoryTypeOwnerMappings) {
			if (mapping.Type == type) {
				return mapping.Owner;
			}
		}

		[[unlikely]] throw std::runtime_error("Invalid CPU type");
	}

	/// <summary>
	/// Get CPU memory type for CPU.
	/// </summary>
	/// <param name="type">CPU type</param>
	/// <returns>Memory type (e.g., SnesMemory for CpuType::Snes)</returns>
	[[nodiscard]] static constexpr MemoryType GetCpuMemoryType(CpuType type) {
		return GetCpuTypeMetadata(type).CpuMemoryType;
	}

	/// <summary>
	/// Get program counter display size (hex digits) for CPU.
	/// </summary>
	/// <param name="type">CPU type</param>
	/// <returns>Hex digit count (4=16-bit, 5=20-bit, 6=24-bit, 8=32-bit)</returns>
	[[nodiscard]] static constexpr int GetProgramCounterSize(CpuType type) {
		return GetCpuTypeMetadata(type).ProgramCounterSize;
	}

	/// <summary>
	/// Convert memory type to CPU type.
	/// </summary>
	/// <param name="type">Memory type</param>
	/// <returns>Owning CPU type</returns>
	[[nodiscard]] static constexpr CpuType ToCpuType(MemoryType type) {
		if (IsRelativeMemory(type)) {
			return GetCpuTypeFromBaseMemoryType(type);
		}

		return GetCpuTypeFromNonRelativeMemoryType(type);
	}

	/// <summary>
	/// Check if memory type is CPU-relative (addressable by CPU).
	/// </summary>
	[[nodiscard]] static constexpr bool IsRelativeMemory(MemoryType memType) {
		return memType <= GetLastCpuMemoryType();
	}

	/// <summary>
	/// Get the PRG ROM memory type for a given CPU type.
	/// Used by LightweightCdlRecorder to determine which resolved addresses are ROM.
	/// </summary>
	/// <param name="cpuType">CPU type</param>
	/// <returns>PRG ROM memory type (e.g., NesPrgRom for NES, SnesPrgRom for SNES)</returns>
	[[nodiscard]] static constexpr MemoryType GetPrgRomMemoryType(CpuType cpuType) {
		return GetCpuTypeMetadata(cpuType).PrgRomMemoryType;
	}

	/// <summary>
	/// Get last CPU memory type enum value.
	/// </summary>
	[[nodiscard]] static constexpr MemoryType GetLastCpuMemoryType() {
		return MemoryType::ChannelFMemory;
	}

	/// <summary>
	/// Check if memory type is PPU memory (VRAM/OAM/palette).
	/// </summary>
	[[nodiscard]] static constexpr bool IsPpuMemory(MemoryType memType) {
		switch (memType) {
			case MemoryType::SnesVideoRam:
			case MemoryType::SnesSpriteRam:
			case MemoryType::SnesCgRam:
			case MemoryType::GbVideoRam:
			case MemoryType::GbSpriteRam:

			case MemoryType::NesChrRam:
			case MemoryType::NesChrRom:
			case MemoryType::NesSpriteRam:
			case MemoryType::NesPaletteRam:
			case MemoryType::NesNametableRam:
			case MemoryType::NesSecondarySpriteRam:
			case MemoryType::NesPpuMemory:
				return true;

			case MemoryType::PceVideoRam:
			case MemoryType::PceVideoRamVdc2:
			case MemoryType::PcePaletteRam:
			case MemoryType::PceSpriteRam:
			case MemoryType::PceSpriteRamVdc2:
				return true;

			case MemoryType::SmsVideoRam:
			case MemoryType::SmsPaletteRam:
				return true;

			case MemoryType::GbaVideoRam:
			case MemoryType::GbaSpriteRam:
			case MemoryType::GbaPaletteRam:
				return true;

			case MemoryType::GenesisVideoRam:
			case MemoryType::GenesisPaletteRam:
				return true;

			default:
				return false;
		}
	}

	/// <summary>
	/// Check if memory type is ROM (read-only).
	/// </summary>
	[[nodiscard]] static constexpr bool IsRom(MemoryType memType) {
		switch (memType) {
			case MemoryType::SnesPrgRom:
			case MemoryType::GbPrgRom:
			case MemoryType::GbBootRom:
			case MemoryType::NesPrgRom:
			case MemoryType::NesChrRom:
			case MemoryType::PcePrgRom:
			case MemoryType::DspDataRom:
			case MemoryType::DspProgramRom:
			case MemoryType::St018PrgRom:
			case MemoryType::St018DataRom:
			case MemoryType::SufamiTurboFirmware:
			case MemoryType::SufamiTurboSecondCart:
			case MemoryType::SpcRom:
			case MemoryType::SmsPrgRom:
			case MemoryType::SmsBootRom:
			case MemoryType::GbaPrgRom:
			case MemoryType::GbaBootRom:
			case MemoryType::WsPrgRom:
			case MemoryType::LynxPrgRom:
			case MemoryType::LynxBootRom:
			case MemoryType::GenesisPrgRom:
				return true;

			case MemoryType::Atari2600PrgRom:
				return true;

			case MemoryType::ChannelFBiosRom:
			case MemoryType::ChannelFCartRom:
				return true;

			default:
				return false;
		}
	}

	[[nodiscard]] static constexpr bool IsVolatileRam(MemoryType memType) {
		if (IsRom(memType)) {
			return false;
		}

		switch (memType) {
			case MemoryType::NesSaveRam:
			case MemoryType::GbCartRam:
			case MemoryType::SnesSaveRam:
			case MemoryType::SufamiTurboSecondCartRam:
			case MemoryType::PceSaveRam:
			case MemoryType::SnesRegister:
			case MemoryType::SmsCartRam:
			case MemoryType::GbaSaveRam:
			case MemoryType::WsCartRam:
			case MemoryType::LynxSaveRam:
				return false;

			default:
				return true;
		}
	}

	/// <summary>
	/// Get last CPU type enum value.
	/// </summary>
	[[nodiscard]] static constexpr CpuType GetLastCpuType() {
		return CpuType::ChannelF;
	}

	/// <summary>
	/// Format address as hexadecimal string for CPU type.
	/// </summary>
	/// <param name="cpuType">CPU type</param>
	/// <param name="address">Address value</param>
	/// <returns>Formatted hex string (e.g., "CAFE" for 16-bit, "12CAFE" for 24-bit)</returns>
	[[nodiscard]] static string AddressToHex(CpuType cpuType, int32_t address) {
		int size = GetProgramCounterSize(cpuType);
		if (size == 4) {
			return HexUtilities::ToHex((uint16_t)address);
		} else if (size == 5) {
			return HexUtilities::ToHex20(address);
		} else if (size == 6) {
			return HexUtilities::ToHex24(address);
		} else if (size == 8) {
			return HexUtilities::ToHex32(address);
		} else {
			return HexUtilities::ToHex(address);
		}
	}

	/// <summary>
	/// Get total memory type count.
	/// </summary>
	/// <returns>Number of memory type enum values</returns>
	[[nodiscard]] static constexpr int GetMemoryTypeCount() {
		return (int)MemoryType::None + 1;
	}

private:
	struct MemoryTypeOwnerMapping {
		MemoryType Type;
		CpuType Owner;
	};

	static constexpr std::array<CpuTypeMetadata, 17> _cpuTypeMetadata = {{
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

	static constexpr std::array<MemoryTypeOwnerMapping, 76> _nonRelativeMemoryTypeOwnerMappings = {{
		{MemoryType::SnesCgRam, CpuType::Snes},
		{MemoryType::SnesPrgRom, CpuType::Snes},
		{MemoryType::SnesSaveRam, CpuType::Snes},
		{MemoryType::SnesSpriteRam, CpuType::Snes},
		{MemoryType::SnesVideoRam, CpuType::Snes},
		{MemoryType::SnesWorkRam, CpuType::Snes},
		{MemoryType::BsxMemoryPack, CpuType::Snes},
		{MemoryType::BsxPsRam, CpuType::Snes},
		{MemoryType::SufamiTurboFirmware, CpuType::Snes},
		{MemoryType::SufamiTurboSecondCart, CpuType::Snes},
		{MemoryType::SufamiTurboSecondCartRam, CpuType::Snes},
		{MemoryType::SnesRegister, CpuType::Snes},
		{MemoryType::SpcRam, CpuType::Spc},
		{MemoryType::SpcRom, CpuType::Spc},
		{MemoryType::SpcDspRegisters, CpuType::Spc},
		{MemoryType::GsuWorkRam, CpuType::Gsu},
		{MemoryType::Sa1InternalRam, CpuType::Sa1},
		{MemoryType::DspDataRam, CpuType::NecDsp},
		{MemoryType::DspDataRom, CpuType::NecDsp},
		{MemoryType::DspProgramRom, CpuType::NecDsp},
		{MemoryType::Cx4DataRam, CpuType::Cx4},
		{MemoryType::St018PrgRom, CpuType::St018},
		{MemoryType::St018DataRom, CpuType::St018},
		{MemoryType::St018WorkRam, CpuType::St018},
		{MemoryType::GbPrgRom, CpuType::Gameboy},
		{MemoryType::GbWorkRam, CpuType::Gameboy},
		{MemoryType::GbCartRam, CpuType::Gameboy},
		{MemoryType::GbHighRam, CpuType::Gameboy},
		{MemoryType::GbBootRom, CpuType::Gameboy},
		{MemoryType::GbVideoRam, CpuType::Gameboy},
		{MemoryType::GbSpriteRam, CpuType::Gameboy},
		{MemoryType::NesChrRam, CpuType::Nes},
		{MemoryType::NesChrRom, CpuType::Nes},
		{MemoryType::NesInternalRam, CpuType::Nes},
		{MemoryType::NesNametableRam, CpuType::Nes},
		{MemoryType::NesMapperRam, CpuType::Nes},
		{MemoryType::NesPaletteRam, CpuType::Nes},
		{MemoryType::NesPpuMemory, CpuType::Nes},
		{MemoryType::NesPrgRom, CpuType::Nes},
		{MemoryType::NesSaveRam, CpuType::Nes},
		{MemoryType::NesSpriteRam, CpuType::Nes},
		{MemoryType::NesWorkRam, CpuType::Nes},
		{MemoryType::PcePrgRom, CpuType::Pce},
		{MemoryType::PceWorkRam, CpuType::Pce},
		{MemoryType::PceSaveRam, CpuType::Pce},
		{MemoryType::PceCdromRam, CpuType::Pce},
		{MemoryType::PceCardRam, CpuType::Pce},
		{MemoryType::PceAdpcmRam, CpuType::Pce},
		{MemoryType::PceArcadeCardRam, CpuType::Pce},
		{MemoryType::PceVideoRam, CpuType::Pce},
		{MemoryType::PceVideoRamVdc2, CpuType::Pce},
		{MemoryType::PcePaletteRam, CpuType::Pce},
		{MemoryType::PceSpriteRam, CpuType::Pce},
		{MemoryType::PceSpriteRamVdc2, CpuType::Pce},
		{MemoryType::SmsPrgRom, CpuType::Sms},
		{MemoryType::SmsWorkRam, CpuType::Sms},
		{MemoryType::SmsCartRam, CpuType::Sms},
		{MemoryType::SmsBootRom, CpuType::Sms},
		{MemoryType::SmsVideoRam, CpuType::Sms},
		{MemoryType::SmsPaletteRam, CpuType::Sms},
		{MemoryType::SmsPort, CpuType::Sms},
		{MemoryType::GbaPrgRom, CpuType::Gba},
		{MemoryType::GbaBootRom, CpuType::Gba},
		{MemoryType::GbaSaveRam, CpuType::Gba},
		{MemoryType::GbaIntWorkRam, CpuType::Gba},
		{MemoryType::GbaExtWorkRam, CpuType::Gba},
		{MemoryType::GbaVideoRam, CpuType::Gba},
		{MemoryType::GbaSpriteRam, CpuType::Gba},
		{MemoryType::GbaPaletteRam, CpuType::Gba},
		{MemoryType::WsPrgRom, CpuType::Ws},
		{MemoryType::WsWorkRam, CpuType::Ws},
		{MemoryType::WsCartRam, CpuType::Ws},
		{MemoryType::WsCartEeprom, CpuType::Ws},
		{MemoryType::WsBootRom, CpuType::Ws},
		{MemoryType::WsInternalEeprom, CpuType::Ws},
		{MemoryType::WsPort, CpuType::Ws},
		{MemoryType::LynxPrgRom, CpuType::Lynx},
		{MemoryType::LynxWorkRam, CpuType::Lynx},
		{MemoryType::LynxBootRom, CpuType::Lynx},
		{MemoryType::LynxSaveRam, CpuType::Lynx},
		{MemoryType::GenesisPrgRom, CpuType::Genesis},
		{MemoryType::GenesisWorkRam, CpuType::Genesis},
		{MemoryType::GenesisVideoRam, CpuType::Genesis},
		{MemoryType::GenesisPaletteRam, CpuType::Genesis},
		{MemoryType::Atari2600PrgRom, CpuType::Atari2600},
		{MemoryType::Atari2600Ram, CpuType::Atari2600},
		{MemoryType::Atari2600TiaRegisters, CpuType::Atari2600},
		{MemoryType::ChannelFBiosRom, CpuType::ChannelF},
		{MemoryType::ChannelFCartRom, CpuType::ChannelF},
		{MemoryType::ChannelFVideoRam, CpuType::ChannelF}
	}};
};
