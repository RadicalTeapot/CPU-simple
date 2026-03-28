# Audio Processing Unit (APU)

This document describes the architecture of the minimal version of the audio chip: its register interface, oscillator model, envelope, and filter. It is the authoritative reference for all audio design decisions.

---

## Overview

The APU is a single-voice synthesizer. It produces one audio stream based on four write-only MMIO registers. The CPU has no way to read back APU state; the registers are fire-and-forget.

The APU is minimal by design — the same philosophy as the `Minimal8Bit` PPU configuration. It provides enough capability to produce music and sound effects without requiring complex runtime management.

---

## MMIO registers

All four registers are **write-only**. The APU occupies 4 MMIO slots (relative offsets `0x00–0x03`). See `mmio.md` for the full address map.

| Rel offset | Name | Bits | Description |
|------------|------|------|-------------|
| `0x00` | `AUDNOTE` | `[7]` gate, `[6:0]` MIDI note | Gate and pitch |
| `0x01` | `AUDENV` | `[3:0]` attack, `[7:4]` release | Envelope timing |
| `0x02` | `AUDFLT` | `[7]` filter mode, `[6:0]` cutoff (MIDI note) | Filter cutoff |
| `0x03` | `AUDCTL` | `[7]` filter slope, `[6]` filter type, `[5:4]` pulse width, `[3:0]` gain/sustain | Oscillator and filter control |

---

## Oscillator

The APU produces a **pulse wave** (square wave with variable duty cycle). Pulse width is set by bits `[5:4]` of `AUDCTL`:

| `PW[1:0]` | Duty cycle |
|-----------|------------|
| `00` | 6.25% |
| `01` | 12.5% |
| `10` | 25% |
| `11` | 50% (square wave) |

Pitch is determined by the MIDI note value in bits `[6:0]` of `AUDNOTE`. The standard formula maps MIDI note N to frequency:

```
f = 440 × 2^((N − 69) / 12)   Hz
```

MIDI note 0 maps to ~8 Hz; note 127 maps to ~12,544 Hz. Note 69 = A4 = 440 Hz. Integer truncation is applied: the result is stored as an `int` (Hz).

---

## Gate

Bit `[7]` of `AUDNOTE` is the **gate**.

- **Gate high (1):** the envelope enters the attack phase and the oscillator runs. Writing a new note value to `AUDNOTE` while the gate is already high changes the pitch immediately without restarting the envelope — the note glides to the new pitch at the current amplitude. This enables legato and portamento effects.
- **Gate low (0):** the envelope enters the release phase. Once release completes the oscillator output is silent. The gate must be brought high again to trigger a new note.

To retrigger a note cleanly (restart the envelope from zero), write `AUDNOTE` with gate=0, then immediately write it again with gate=1 and the new note value.

---

## Envelope (ASR)

The APU uses an **Attack–Sustain–Release** (ASR) envelope, not ADSR. There is no separate decay phase; amplitude rises during attack and holds at the sustain level until the gate drops.

```
amplitude
   ▲                sustain
   │   '   ─────────────────────────
   │   '  /                         '\
   │   ' /attack               release\
   │   '/                           '  \
   └───└────────────────────────────└─────►time
    gate high                   gate low
```

- **Attack** (`AUDENV[3:0]`, 4 bits, lower nibble): time to rise from 0 to maximum amplitude.
- **Sustain** (`AUDCTL[3:0]`, 4 bits): amplitude level held after attack completes.
- **Release** (`AUDENV[7:4]`, 4 bits, upper nibble): time to fall from sustain level to 0 after gate drops.

The envelope uses a **linear ramp** (not exponential). The attack ramps from 0 to 1 linearly over the attack time; release ramps from 1 to 0 linearly over the release time.

### Attack times (seconds)

The 4-bit attack value indexes into a SID-derived lookup table:

| Value | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 |
|-------|------|------|------|------|------|------|------|------|-----|------|-----|-----|-----|-----|-----|-----|
| Seconds | 0.002 | 0.008 | 0.016 | 0.024 | 0.038 | 0.056 | 0.068 | 0.080 | 0.1 | 0.24 | 0.5 | 0.8 | 1.0 | 3.0 | 5.0 | 8.0 |

### Release times (seconds)

The 4-bit release value indexes into a SID-derived lookup table:

| Value | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 |
|-------|------|------|------|------|------|------|------|------|-----|------|-----|-----|-----|-----|-----|-----|
| Seconds | 0.006 | 0.024 | 0.048 | 0.072 | 0.114 | 0.168 | 0.204 | 0.24 | 0.3 | 0.75 | 1.5 | 2.4 | 3.0 | 9.0 | 15.0 | 24.0 |

### Sustain level

The 4-bit sustain nibble is converted linearly to a `[0.0, 1.0]` amplitude multiplier:

```
sustain_level = nibble / 15
```

`0x0` = silent (0.0), `0xF` = maximum (1.0). The sustain level acts as the peak the attack ramps toward and the starting level for release.

---

## Filter

The filter is a biquad **low-pass** or **high-pass** filter. Frequencies above (LP) or below (HP) the cutoff are attenuated.

### Cutoff frequency

Bit `[7]` of `AUDFLT` selects the filter mode:

| `AUDFLT[7]` | Mode |
|-------------|------|
| `0` | Fixed cutoff — the cutoff frequency opens instantly on gate-high and closes instantly on gate-low at the value written to `AUDFLT[6:0]` |
| `1` | ASR-modulated cutoff — the cutoff ramps from 0 to the written value using the same attack/release timing as the note envelope (`AUDENV`) |

The cutoff frequency is expressed as a **MIDI note value** (`[6:0]`, 0–127), converted with the same formula as pitch. This keeps the filter musically in tune with the oscillator.

### Filter type

Bit `[6]` of `AUDCTL` selects the filter type:

| `AUDCTL[6]` | Mode |
|-------------|------|
| `0` | Low pass filter |
| `1` | High pass filter |

### Filter slope

Bit `[7]` of `AUDCTL` selects the filter slope:

| `AUDCTL[7]` | Mode |
|-------------|------|
| `0` | 2 pole (12 dB/octave) |
| `1` | 4 pole (24 dB/octave) |

The filter is implemented as a Direct Form II Transposed biquad (numerically stable). The 24 dB/octave slope cascades two biquad stages.

---

## Register interaction summary

| Action | Registers written |
|--------|-------------------|
| Start a note | `AUDENV` (timing), `AUDCTL` (PW, gain, filter), `AUDFLT` (filter cutoff), then `AUDNOTE` with gate=1 |
| Change pitch without retriggering | `AUDNOTE` with gate=1 and new note value |
| Stop a note (enter release) | `AUDNOTE` with gate=0 (note value ignored on release) |
| Retrigger (restart envelope) | `AUDNOTE` gate=0, then gate=1 with new note value |
| Sweep filter manually | Write new value to `AUDFLT` mid-note (fixed mode only) |

---

## Emulator integration

The APU is automatically enabled whenever the PPU is active (`--vram` flag). Two optional flags configure audio output:

| Flag | Config key | Default | Description |
|------|------------|---------|-------------|
| `--sample-rate HZ` | `SampleRate` | `44100` | Output sample rate in Hz |
| `--buffer-size N` | `AudioBufferSize` | `1024` | Raylib audio stream buffer size (samples) |

Neither flag can be used without `--vram`. The CPU clock rate used internally by the APU is derived from the PPU configuration at startup (`TotalScanlines × CyclesPerScanline / PpuCyclesPerCpuCycle × 60 fps`) and is not user-configurable.

Audio samples are generated in a batch at the end of each video frame (after PPU `FrameReady` fires). Raylib consumes one buffer's worth of samples per frame via `IsAudioStreamProcessed` / `UpdateAudioStream`.

### emulator.json example

```json
{
  "vram": 256,
  "SampleRate": 44100,
  "AudioBufferSize": 1024
}
```

---

## What is not in scope

- Multiple voices / polyphony
- Noise channel or triangle wave
- DMA-based sample playback
- Per-voice volume (gain/sustain is the only amplitude control)
- Hardware mixing (single voice means no mixing needed)
