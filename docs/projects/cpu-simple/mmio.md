# MMIO (Memory-Mapped I/O)

This document describes the multi-device MMIO architecture: how IO devices are registered, how the CPU reaches them, and what the full address maps look like in both builds. It is the authoritative reference for all MMIO design decisions.

---

## Overview

The CPU has a single address bus. An address emitted by an opcode may target main RAM or an IO device. A **bus decoder** sits between the CPU and all addressable components and routes each access to the correct target based on address range:

```
CPU
 │
 ▼
BusDecoder ──── 0x00–0xE7 ──▶ main RAM          (8-bit)
              ── 0xE8–0xEF ──▶ MmioRouter ──▶ PPU registers
                                           ──▶ Gamepad register
                                           ──▶ Audio registers
              ── 0xF0–0xFF ──▶ Reserved (unmapped stack space)

BusDecoder ──── 0x0000–0xEEFF ──▶ main RAM      (16-bit)
              ── 0xEF00–0xEFFF ──▶ MmioRouter ──▶ PPU registers
                                              ──▶ Gamepad register
                                              ──▶ Audio registers
              ── 0xFF00–0xFFFF ──▶ Reserved (unmapped stack space)
```

The `BusDecoder` only distinguishes RAM from the MMIO region. Within the MMIO region, routing to individual devices is the responsibility of `MmioRouter`.

---

## MmioRouter

`MmioRouter` implements `IMmioDevice` and composes multiple IO devices, each assigned a contiguous sub-range of the MMIO address space.

### Registration

Each device is registered with a base offset and a size:

```csharp
router.Register(baseOffset: 0x00, size: 3, device: ppuRegisters);
router.Register(baseOffset: 0x03, size: 1, device: gamepad);
router.Register(baseOffset: 0x04, size: 4, device: audio);
```

The router subtracts the device's base offset before forwarding, so each device always receives **relative offsets starting at 0x00**. Devices do not need to know where they sit in the global MMIO map.

### Dispatch rules

| Condition | Read | Write |
|-----------|------|-------|
| Address falls in a registered device's range | Forward to device with relative offset | Forward to device with relative offset |
| Address is unmapped | Return `0x00` | Silently ignored |

### Invariants enforced at registration time

- **No overlapping ranges.** Registering a device whose range intersects any existing registration throws immediately.
- Registrations are permanent for the lifetime of the router; devices cannot be removed or re-registered.

### Relation to existing components

| Component | Change required |
|-----------|----------------|
| `IMmioDevice` | None |
| `BusDecoder` | None |
| `NullMmioDevice` | Redundant — an empty `MmioRouter` serves the same role |
| `CPU.CPU` | None |
| Individual IO devices (`PpuRegisters`, etc.) | None |

`MmioRouter` lives in the `CPU` project alongside `NullMmioDevice` and `BusDecoder`. It depends only on `IMmioDevice` and has no knowledge of specific devices.

---

## 8-bit MMIO address map

MMIO region: `0xE8–0xEF` (8 slots, offsets `0x00–0x08`).

| Abs offset | Device | Rel offset | Register name | Direction |
|------------|--------|------------|---------------|-----------|
| `0xE8` | PPU | `0x00` | `PPUADDR` | W |
| `0xE9` | PPU | `0x01` | `PPUDATA` | W |
| `0xEA` | PPU | `0x02` | `PPUSTATUS` | R |
| `0xEB` | Gamepad | `0x00` | `GAMEPAD` | R |
| `0xEC` | Audio | `0x00` | `AUDNOTE` | W |
| `0xED` | Audio | `0x01` | `AUDENV` | W |
| `0xEE` | Audio | `0x02` | `AUDFLT` | W |
| `0xEF` | Audio | `0x03` | `AUDCTL` | W |

Device registration:

```
PPU      base=0x00 size=3
Gamepad  base=0x03 size=1
Audio    base=0x04 size=4
```

---

## 16-bit MMIO address map

MMIO region: `0xEF00–0xEFFF` (256 slots, offsets `0x00–0xFF`).

| Abs offset | Device | Rel offset | Register name | Direction |
|------------|--------|------------|---------------|-----------|
| `0xEF00` | PPU | `0x00` | `PPUADDR` | W |
| `0xEF01` | PPU | `0x01` | `PPUDATA` | W |
| `0xEF02` | PPU | `0x02` | `PPUSTATUS` | R |
| `0xEF03` | PPU | `0x03` | `PPUBDR` | W |
| `0xEF04` | PPU | `0x04` | `PPUBDG` | W |
| `0xEF05` | PPU | `0x05` | `PPUBDB` | W |
| `0xEF06` | Gamepad | `0x00` | `GAMEPAD` | R |
| `0xEF07` | Audio | `0x00` | `AUDNOTE` | W |
| `0xEF08` | Audio | `0x01` | `AUDENV` | W |
| `0xEF09` | Audio | `0x02` | `AUDFLT` | W |
| `0xEF0A` | Audio | `0x03` | `AUDCTL` | W |
| `0xEF0B–0xEFFF` | — | — | Reserved | — |

Device registration:

```
PPU      base=0x00 size=6
Gamepad  base=0x06 size=1
Audio    base=0x07 size=4
```

Used: 11 of 256 slots. Reserved: 245 slots.

> **Note:** `CpuHandler` currently registers devices at the same fixed offsets in both builds (PPU 0x00/3, Gamepad 0x03/1, Audio 0x04/4). In 16-bit mode this places audio at `0xEF04–0xEF07`, not `0xEF07–0xEF0A` as shown above. The table above reflects the intended design; the implementation will be aligned when the 16-bit PPU register set is fully wired.

---

## Watchpoints on MMIO registers

CPU writes to MMIO registers pass through `BusDecoder` as ordinary address-space writes and are recorded in `TickTrace` with `BusType.Memory`. Existing `AddressWatchpoint` already triggers on these addresses without modification.

Example: `wp write 0xE8` breaks on any CPU store to `PPUADDR` in 8-bit mode.

A convenience alias (`wp mmio <name>`) expanding to the known register address would be a UX improvement but is not required for correctness.
