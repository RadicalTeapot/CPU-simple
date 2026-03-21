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
| `0x01` | `AUDENV` | `[7:4]` attack, `[3:0]` release | Envelope timing |
| `0x02` | `AUDFLT` | `[7]` filter mode, `[6:0]` cutoff (MIDI note) | Low-pass filter |
| `0x03` | `AUDCTL` | `[7]` filter slope, `[6]` filter type, `[5:4]` pulse width, `[3:0]` gain/sustain | Oscillator and filter control |

---

## Oscillator

The APU produces a **pulse wave** (square wave with variable duty cycle). Pulse width is set by bits `[5:4]` of `AUDCTL`:

| `PW[1:0]` | Duty cycle |
|-----------|------------|
| `00` | 6% |
| `01` | 12.5% |
| `10` | 25% |
| `11` | 50% (square wave) |

Pitch is determined by the MIDI note value in bits `[6:0]` of `AUDNOTE`. The standard formula maps MIDI note N to frequency:

```
f = 440 × 2^((N − 69) / 12)   Hz
```

MIDI note 0 maps to ~8.18 Hz; note 127 maps to ~12,543 Hz. Note 69 = A4 = 440 Hz.

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

- **Attack** (`AUDENV[7:4]`, 4 bits): time to rise from 0 to maximum amplitude. 16 steps.
- **Sustain** (`AUDCTL[3:0]`, 4 bits): amplitude level held after attack completes. `0x0` = silent, `0xF` = maximum. Also called "gain" — it sets the peak the attack ramps toward.
- **Release** (`AUDENV[3:0]`, 4 bits): time to fall from the current sustain level to 0 after the gate drops. 16 steps.

The exact timing curve (linear vs. exponential, milliseconds per step) is TBD and will be defined in the implementation. Real hardware (e.g., SID chip) uses exponential curves for a more natural decay; a linear approximation is acceptable for a first implementation.

---

## Filter

The filter is a **low-pass filter** or **high-pass filter**. Frequencies above the cutoff are attenuated; frequencies below pass through.

Bit `[7]` of `AUDFLT` selects the filter mode:

| `AUDFLT[7]` | Mode |
|-------------|------|
| `0` | Fixed cutoff — the cutoff frequency is static at the value written to `AUDFLT[6:0]` |
| `1` | ASR-modulated cutoff — the cutoff frequency follows the **same ASR envelope** as the oscillator amplitude (same attack time, same sustain level proportionally, same release time). The cutoff sweeps from 0 to `AUDFLT[6:0]` during attack, holds at `AUDFLT[6:0]` during sustain, and sweeps back to 0 during release. |

The cutoff frequency is expressed as a **MIDI note value** (`[6:0]`, 0–127), using the same formula as pitch. This keeps the filter musically in tune with the oscillator — a cutoff at note N is always harmonically related to a pitch also set to note N.

In ASR mode, `AUDFLT[6:0]` defines the **peak cutoff** (the sustain level of the filter envelope). In fixed mode, the cutoff register can be updated mid-note by the CPU for manual filter sweeps.

Bit `[6]` of `AUDCTL` selects the filter type:

| `AUDFLT[6]` | Mode |
|-------------|------|
| `0` | Low pass filter |
| `1` | High pass filter |

Bit `[7]` of `AUDCTL` selects the filter slope:

| `AUDFLT[7]` | Mode |
|-------------|------|
| `0` | 2 pole (12dB) |
| `1` | 4 pole (24dB) |

---

## Register interaction summary

| Action | Registers written |
|--------|-------------------|
| Start a note | `AUDENV` (timing), `AUDCTL` (PW, gain), `AUDFLT` (filter), then `AUDNOTE` with gate=1 |
| Change pitch without retriggering | `AUDNOTE` with gate=1 and new note value |
| Stop a note (enter release) | `AUDNOTE` with gate=0 (note value ignored on release) |
| Retrigger (restart envelope) | `AUDNOTE` gate=0, then gate=1 with new note value |
| Sweep filter manually | Write new value to `AUDFLT` mid-note (fixed mode only) |

---

## What is not in scope

- Multiple voices / polyphony
- Noise channel or triangle wave
- DMA-based sample playback
- Per-voice volume (gain/sustain is the only amplitude control)
- Hardware mixing (single voice means no mixing needed)
