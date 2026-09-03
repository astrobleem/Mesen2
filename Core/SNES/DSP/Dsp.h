#pragma once
#include "pch.h"
#include "SNES/DSP/DspVoice.h"
#include "SNES/DSP/DspTypes.h"
#include "Utilities/ISerializable.h"
#include <cstdio>

class Emulator;
class SnesConsole;
class Spc;

class Dsp final : public ISerializable
{
private:
	DspState _state = {};
	DspVoice _voices[8] = {};
	Emulator* _emu = nullptr;
	Spc* _spc = nullptr;

	//Output buffer
	uint16_t _outSampleCount = 0;
	int16_t _dspOutput[0x2000] = {};

	// Test-only passive per-voice capture. Enabled only when the environment
	// variable MESEN_VOICE_CAPTURE_PATH is set; it never changes DSP state.
	FILE* _voiceCaptureFile = nullptr;
	FILE* _voiceCaptureMetaFile = nullptr;
	FILE* _voiceCaptureTraceFile = nullptr;
	uint8_t _voiceCaptureIndex = 7;
	char _voiceCaptureArmPath[512] = {};
	bool _voiceCaptureActive = false;
	bool _voiceCaptureEndPending = false;
	bool _voiceCaptureInitAttempted = false;
	uint64_t _voiceCaptureSampleIndex = 0;
	uint64_t _voiceCaptureKonIndex = UINT64_MAX;
	uint64_t _voiceCaptureKoffIndex = UINT64_MAX;
	uint64_t _voiceCaptureEndIndex = UINT64_MAX;

	void InitVoiceCapture();
	void FinishVoiceCapture();

	void UpdateCounter();
	
	int32_t CalculateFir(int index, int ch);

	void EchoStep22();
	void EchoStep23();
	void EchoStep24();
	void EchoStep25();
	void EchoStep26();
	void EchoStep27();
	void EchoStep28();
	void EchoStep29();
	void EchoStep30();

public:
	Dsp(Emulator* emu, SnesConsole* console, Spc* spc);
	~Dsp();

	void LoadSpcFileRegs(uint8_t* regs);
	void Reset();

	DspState& GetState() { return _state; }
	bool IsMuted() { return false; }

	uint16_t GetSampleCount() { return _outSampleCount; }
	int16_t* GetSamples() { return _dspOutput; }
	void ResetOutput() { _outSampleCount = 0; }

	void CaptureVoiceEvent(uint8_t voiceIndex, const char* eventName);
	void CaptureVoiceSample(uint8_t voiceIndex, int32_t left, int32_t right);

	bool CheckCounter(int32_t rate);

	uint8_t Read(uint8_t reg) { return _state.ExternalRegs[reg]; }
	void Write(uint8_t reg, uint8_t value);

	uint8_t ReadReg(DspGlobalRegs reg) { return _state.Regs[(int)reg]; }
	
	void WriteGlobalReg(DspGlobalRegs reg, uint8_t value)
	{
		_state.ExternalRegs[(int)reg] = value;
		_state.Regs[(int)reg] = value;
	}

	void WriteVoiceReg(uint8_t voiceIndex, DspVoiceRegs reg, uint8_t value)
	{
		_state.ExternalRegs[voiceIndex * 0x10 + (int)reg] = value;
		_state.Regs[voiceIndex * 0x10 + (int)reg] = value;
	}

	static int16_t Clamp16(int32_t val)
	{
		if(val < INT16_MIN) {
			return INT16_MIN;
		} else if(val > INT16_MAX) {
			return INT16_MAX;
		}
		return val;
	}

	void Exec();

	void Serialize(Serializer& s) override;
};
