# PPU (Picture Processing Unit)

This document describes the architecture of the PPU: its relationship with the CPU, how data flows in and out, and how it produces pixels on screen. It is the authoritative reference for PPU design decisions in this project.

---

## Overview

The PPU is a co-processor dedicated to graphics. It has its own **VRAM** (Video RAM), its own internal bus, and runs its own rendering pipeline independently of the CPU. The CPU communicates with the PPU through a small set of **memory-mapped I/O registers** — the only window into the PPU from the CPU's perspective.

This design preserves the CPU's address space (256 bytes in 8-bit mode, 64KB in 16-bit mode) for program code and data. VRAM can be as large as needed without consuming any CPU address space.

---

## Memory-Mapped I/O (MMIO)

### How it works

The CPU has a single address bus. When an opcode executes `sta r0, [0xF8]`, the bus emits a write to address `0xF8`. Something must decide: does this go to main RAM, or to a PPU register?

This decision is made by a **bus decoder** (also called a memory mapper or MMIO controller) that sits between the CPU and all addressable devices. It inspects the address of every read and write and routes it to the correct target:

```
CPU
 │
 ▼
Bus Decoder  ──── 0x00–0xEF ──▶ main RAM
                ── 0xF0–0xFF ──▶ PPU registers
```

The decoder intercepts **both reads and writes**. This is important: some PPU registers are readable (e.g., a status register reporting whether VBlank is active, or whether a sprite-overflow occurred). The decoder must handle both directions.

### Implementation note

In this project, `CPU/components/Memory.cs` currently handles all reads and writes. The bus decoder should be introduced as a **separate layer** with the same read/write interface as `Memory`, so it can be injected into the opcode system as a drop-in replacement. The decoder then holds references to both `Memory` and the `PPU`, and forwards calls based on address range. This keeps `Memory` single-responsibility.

The stack is **not** part of this routing. Stack operations (`Push`/`Pop`) bypass the address bus entirely — they go through the `Stack` component directly in opcode implementations.

---

## Writing Data to VRAM: the Address/Data Register Pair

The CPU cannot address VRAM directly. To write bulk data (tile patterns, palettes, tilemaps) into VRAM, the PPU exposes two special MMIO registers:

| Register  | Purpose |
|-----------|---------|
| `PPUADDR` | Sets the target address in VRAM for the next write |
| `PPUDATA` | Writes one byte to VRAM at the current address, then auto-increments the address |

The CPU upload loop looks like:

```
; Set VRAM destination address
ldi r0, #TILE_DATA_VRAM_ADDR
sta r0, [PPUADDR]

; Write tile bytes one at a time
ldi r1, #TILE_BYTE_COUNT
ldi r2, #tile_data_start
upload_loop:
  lda r3, [r2]        ; load next byte from CPU RAM
  sta r3, [PPUDATA]   ; write it to VRAM (auto-increments PPUADDR)
  inc r2
  dec r1
  jnz [upload_loop]
```

After each write to `PPUDATA`, the PPU increments its internal VRAM address pointer automatically. To jump to a non-contiguous VRAM region, write a new value to `PPUADDR`.

### The PPUADDR width problem

If VRAM is larger than 256 bytes, a single 8-bit `PPUADDR` register cannot address all of it. The NES solved this with a **write latch**: `PPUADDR` requires two consecutive writes — high byte first, then low byte — to set a full 16-bit address. The PPU maintains an internal 1-bit toggle to know which byte it is receiving.

For this project's 8-bit build with modest VRAM, a single-byte address may suffice. In 16-bit mode (`#if x16`), the two-write latch mechanism (or a second `PPUADDRHIGH` register) is needed. This is a concrete design decision.

### VBlank is a budget, not unlimited time

All VRAM uploads must happen during **VBlank** (see below). VBlank lasts a fixed number of CPU cycles. If more data needs to be uploaded than fits in one VBlank window, the upload must be spread across multiple frames, limiting how much can change per frame.

---

## Tile-Based Rendering

### Why not a framebuffer?

A raw framebuffer for a 256×240 screen at 8-bit color depth requires:

```
256 × 240 × 1 byte = 61,440 bytes ≈ 60 KB
```

This is more than the entire CPU address space in 8-bit mode, and nearly all of it in 16-bit mode. Even reading and transmitting 60KB per frame to the PPU would saturate the bus. Framebuffers are not viable for 8-bit systems.

### Tiles

A **tile** is a fixed-size block of pixel data, typically 8×8 pixels. Rather than storing pixel colors directly, each pixel stores a **palette index** — an index into a small color lookup table. With 4 colors per tile (2 bits per pixel), one 8×8 tile occupies:

```
8 × 8 × 2 bits = 128 bits = 16 bytes
```

A library of 256 distinct tile patterns requires only **4 KB** of VRAM (versus 60KB for a raw framebuffer).

### The nametable (tilemap)

The tile pattern data alone doesn't describe what the screen looks like. The PPU also needs to know *which tile goes where*. This is stored in a **nametable**: a 2D grid of tile indices.

For a 256×240 screen with 8×8 tiles:

```
32 columns × 30 rows = 960 bytes
```

960 bytes describes the complete background layout. Each byte is an index into the 256-tile CHR library. This is the key insight: the screen layout costs under 1 KB.

Total VRAM for background rendering: ~5 KB (4 KB CHR + 960 B nametable) versus 60 KB for a framebuffer. That is a 12× reduction.

### Tile reuse

The same tile pattern can appear at any number of nametable positions. A brick-wall background might consist of 900 tiles that all reference the same 16-byte pattern. Reuse is free — the nametable just repeats the same index.

### Pixel bit depth

Tiles typically use 2 bits per pixel (4 colors selected from a palette) rather than 8 bits. This bit-depth reduction is independent of reuse and contributes significantly to the overall VRAM savings.

---

## Sprites and OAM

### Sprites vs tiles

A **tile** is a graphical pattern stored in VRAM (CHR data). A **sprite** is a screen object that references a tile pattern and adds position and display attributes. Multiple sprites can reference the same tile pattern — the pattern is uploaded once, the per-sprite data is small.

### OAM (Object Attribute Memory)

Sprite instances are stored in **OAM** — a dedicated region of memory inside the PPU (not VRAM proper). A typical OAM entry is 4 bytes:

| Field | Size | Notes |
|-------|------|-------|
| Y position | 1 byte | Vertical position on screen |
| Tile index | 1 byte | Which CHR tile pattern to use |
| X position | 1 byte | Horizontal position on screen |
| Attributes | 1 byte | See below |

The attributes byte typically packs:

| Bits | Meaning |
|------|---------|
| 1–0 | Sub-palette index (which palette to use) |
| 5 | Priority: 0 = in front of background, 1 = behind background |
| 6 | Horizontal flip |
| 7 | Vertical flip |

### Flip flags

Horizontal and vertical flip bits allow a single tile pattern to serve up to 4 orientations without storing additional CHR data. A character walking left is the walking-right tile flipped horizontally. Without flip flags, symmetric sprites would require double the CHR VRAM.

### Hiding sprites

Sprites are not made invisible with a dedicated flag. The standard approach is to place the sprite at an off-screen Y coordinate (e.g., Y=255). This is simpler to implement in the PPU's sprite evaluation logic.

### Sprite priority between sprites

When two sprites overlap, priority is determined by their index in OAM — lower index wins. This is a hardware convention, not a per-entry field.

---

## Rendering Pipeline: Scanlines

The PPU does not render the whole frame at once. It renders one **scanline** at a time, from top to bottom, left to right, one pixel per PPU clock cycle.

### Frame structure

A frame is divided into three phases:

```
Scanline 0–239   Active rendering  (visible lines, pixel output)
Scanline 240     Post-render line  (idle)
Scanline 241+    VBlank            (CPU update window, no VRAM reads)
```

### HBlank: the gap between scanlines

At the end of each visible scanline there is a **horizontal blank (HBlank)** period during which the PPU is not outputting pixels. This is used for two things:
- **Sprite evaluation** for the *next* scanline (see below)
- **Tile prefetch** for the first tiles of the next scanline

Sprite evaluation does not happen at the start of the scanline being rendered — it happens during the HBlank of the *previous* scanline. By the time pixel output begins, the list of active sprites is already prepared.

### Per-scanline sequence

The full sequence, in order:

1. **Sprite evaluation (during previous scanline's HBlank)** — The PPU scans all 64 OAM entries and builds a short list of sprites whose Y range overlaps the upcoming scanline. Typically capped at 8 sprites per scanline (a hardware constraint; exceeding it sets a sprite-overflow flag). This produces a small working set used in step 3.

2. **Pipelined tile fetch (interleaved with pixel output)** — The PPU does not load all tile data for the scanline upfront. As it outputs pixels for tile N, it is simultaneously fetching the CHR row and nametable index for tile N+1. Every 8 pixels the pipeline shifts: the prefetched tile becomes active, and the next tile begins fetching. This requires a tight, predictable internal bus schedule — the PPU is never idle during active rendering.

3. **Pixel output** — For each pixel position, the PPU executes a short priority resolution:
   - **Background pixel**: tile index from nametable → CHR row → palette index → color. If the palette index is 0 (transparent), the background color is used.
   - **Sprite pixel**: check the evaluated sprite list for any sprite covering this X position → CHR row → palette index → color. Palette index 0 is transparent for sprites too.
   - **Priority**: if a sprite pixel is opaque and the sprite's priority bit is "in front", the sprite color wins. If the priority bit is "behind" and the background pixel is opaque, the background wins. Sprite-vs-sprite: lower OAM index wins.
   - **Output**: final color sent to the display.

### VBlank

After the last visible scanline (and the post-render idle line), the PPU enters **VBlank** — a period during which it reads nothing from VRAM or OAM. This is the only safe window for the CPU to update PPU state.

At the start of VBlank:
1. The PPU sets a **VBlank flag** in its status register (readable by the CPU via MMIO).
2. The PPU calls `CPU.RequestInterrupt()` — the CPU's VBlank ISR is queued.
3. The CPU's ISR runs: uploads tile data via PPUADDR/PPUDATA, updates the nametable, rewrites OAM, adjusts scroll registers.
4. VBlank ends; the PPU clears the VBlank flag and resets to scanline 0 for the next frame.

Because `CPU.RequestInterrupt()` is already implemented on this branch, VBlank interrupt integration is nearly free — the PPU just needs to call it at the right moment in its scanline counter.

### Mid-frame register writes

Writes to PPU control registers (scroll position, palette, nametable base address) take effect at the **next scanline boundary**, not immediately. Writing mid-scanline causes visual corruption only on the scanlines rendered *after* the write, not before. Some programs exploit this deliberately to produce raster effects — changing the scroll register at a specific scanline produces a split-screen with different scroll values per region. The PPU should apply register changes at scanline boundaries, not mid-pixel.

---

## 8-bit vs 16-bit Build Considerations

### VRAM size is independent of CPU address width

VRAM lives on the PPU's own bus and does not consume CPU address space. Its size can be chosen independently of the `x16` build flag. A reasonable baseline: 2–4 KB in either build, expandable in 16-bit mode if larger tile sets are desired.

### Write-latch: follow VRAM size, not build mode

The PPUADDR write-latch mechanism is needed when VRAM exceeds 256 bytes (a single register can no longer address all of it). The decision rule:

| VRAM size | PPUADDR mechanism |
|-----------|-------------------|
| ≤ 256 bytes | Single 8-bit write, no latch |
| > 256 bytes | Two-write latch (high byte then low byte) |

This is independent of the `x16` flag. A small VRAM in 8-bit mode needs no latch; a large VRAM in either mode does.

### Data width vs address width

The `x16` flag changes the **address bus width** (1-byte vs 2-byte operands in instructions), not the data bus width. The CPU data bus is 8 bits in both builds. Therefore MMIO register *values* (PPUDATA, control registers) are always 8-bit. What `x16` changes is the *address* used to reach those registers: a 1-byte address in 8-bit mode, a 2-byte address in 16-bit mode.

### Address space pressure in 8-bit mode

In 8-bit mode the entire CPU address space is 256 bytes, shared between code, RAM, stack, and MMIO. Every PPU register claims one slot. Eight PPU registers at `0xF8`–`0xFF` leave 248 bytes for everything else — a meaningful constraint. This directly limits how feature-rich the PPU can be in 8-bit mode: more features require more registers, which further shrinks usable RAM.

In 16-bit mode the 64 KB address space makes this a non-issue; the same 8–16 register slots are negligible overhead.

### Register multiplexing as an escape hatch

To reduce MMIO slot usage, the PPU can expose only PPUADDR/PPUDATA and map configuration (palette, control flags, scroll) to special reserved VRAM address ranges rather than dedicated registers. The CPU writes configuration by pointing PPUADDR at a control address and writing via PPUDATA. This trades address slot count for VRAM layout complexity and debuggability: the PPU must distinguish control writes from tile data writes by address range, and the assembly programmer must know which VRAM addresses are "magic." For an educational project, dedicated control registers with a fixed MMIO map are clearer.

---

## Summary: VRAM Layout

A typical VRAM layout for this PPU:

| Region | Contents | Typical size |
|--------|----------|-------------|
| CHR data | Tile patterns (2bpp, 8×8 px each) | 4 KB (256 tiles) |
| Nametable | Background tile index grid (32×30) | 960 bytes |
| Attribute table | Per-tile-block palette assignments | 64 bytes |
| Palettes | Color lookup tables | 32 bytes |
| OAM | Sprite instances (4 bytes each, 64 sprites) | 256 bytes |

Total: approximately **5.3 KB** for a complete scene, versus 60 KB for a raw framebuffer.

---

## Clocking and Synchronization

### Independent clocks

The CPU and PPU run at independent clock rates. In real hardware these rates are often fixed multiples of each other (e.g., the NES PPU runs at exactly 3× the CPU clock frequency). This ratio determines:
- How many PPU ticks occur per CPU tick
- How many CPU cycles fit inside one VBlank window
- How precisely mid-frame timing effects (raster tricks) can be controlled from CPU code

The ratio is a concrete design decision for this project and must be documented in `Config.cs` alongside memory sizes and IRQ addresses.

### VBlank interrupt: notification, not synchronization

The interrupt the PPU fires at the start of VBlank is a **notification** — it tells the CPU "VBlank has started, you may now safely update PPU state." It does not synchronize the two clocks. The clocks remain independent before and after the interrupt. Nothing in the hardware prevents a buggy CPU program from writing to PPU registers mid-frame; the interrupt is an application-level protocol, not a hardware lock.

### The real synchronization concern: data races in C#

In a C# simulation with two threads (one for the CPU, one for the PPU), both threads may access the same MMIO register object simultaneously — the CPU thread writing, the PPU thread reading. This is a **data race** and causes undefined behavior regardless of whether the simulated program is well-behaved.

This requires implementation-level synchronization:
- `lock` blocks around register reads/writes
- `volatile` fields for single-value registers read atomically
- `Interlocked` operations for counters

This concern is separate from the VBlank protocol. Even a perfectly written assembly program that only updates PPU registers during VBlank does not eliminate the data race — the C# simulation must protect shared state independently.

### Simpler alternative: single-threaded co-simulation

Rather than two real OS threads, the simulation can use a **single-threaded co-simulation loop**:

```csharp
while (running)
{
    cpu.Tick();           // advance CPU by 1 cycle
    ppu.Tick();           // advance PPU by 1 cycle (or N ticks per CPU tick per clock ratio)
    ppu.Tick();
    ppu.Tick();
}
```

Each iteration advances both components in a fixed ratio. There is no thread concurrency, so no data races and no synchronization primitives needed. VBlank is detected by the PPU during its own `Tick()` and it calls `cpu.RequestInterrupt()` directly.

This is the recommended approach for an educational simulator. Two real threads add complexity (synchronization, non-determinism, OS scheduling jitter) with no benefit when the goal is accuracy, not wall-clock performance.

---

## Debugger Integration

### Tick traces: separate streams for CPU and PPU

The existing `TickTrace` records CPU bus transactions — memory reads/writes, stack operations — one entry per CPU micro-tick. PPU internal VRAM accesses (tile fetches, sprite evaluation reads) happen on a completely separate bus that the CPU never sees. Including them in `TickTrace` would conflate two unrelated buses and make traces unreadable.

PPU internal activity requires its own **PPU trace stream**, parallel to but separate from `TickTrace`. The Backend's debugger output and any future IDE visualization would show CPU traces and PPU traces independently.

### Watchpoints

**CPU writes to PPU registers** are already covered by the existing `AddressWatchpoint`. Since MMIO register writes pass through the bus decoder as normal address-space writes, `wp write 0xF8` already triggers on a CPU store to that address. No new mechanism is needed — only a convenience alias (`wp ppureg <name>`) that expands to the known register address would be a UX improvement.

**PPU-internal watchpoints** (break on VBlank, HBlank, pixel rendered, specific scanline, register read by PPU) need a new watchpoint family mirroring the existing `IWatchpoint` / `WatchpointContainer` pattern:

- A `PpuWatchpointContainer` holding `IPpuWatchpoint` instances
- Concrete types: `VBlankWatchpoint`, `ScanlineWatchpoint`, `PpuRegisterWatchpoint`, etc.
- Evaluated inside `ppu.Tick()` against a `PpuTickTrace` (the PPU's equivalent of `TickTrace`)
- New `watchpoint` sub-commands to create and manage them (e.g., `wp ppu vblank`, `wp ppu scanline 120`)

### Halting on a PPU watchpoint

When a PPU watchpoint condition is met inside `ppu.Tick()`, the simulation must stop. The co-simulation loop calls `ppu.Tick()` N times per CPU tick:

```csharp
void TickBoth()
{
    cpu.Tick();
    for (int i = 0; i < PpuCyclesPerCpuCycle; i++)
        ppu.Tick();   // watchpoint may fire here
}
```

The PPU needs a way to signal "halt requested" back to the caller so the loop can break out and transition to `IdleState`. The cleanest approach is a return value from `ppu.Tick()` (e.g., `PpuTickResult` with a `HaltRequested` flag), which `TickBoth()` checks after each PPU tick — matching how `ExecutingCpuState` checks watchpoint results after `cpu.Tick()`.

### Ownership and integration with the Backend state machine

The **Backend** owns the PPU — it is the orchestration layer that hosts both the CPU and the PPU. The existing execution states (`SteppingState`, `TickingState`, `RunningState`) each call `cpu.Step()` or `cpu.Tick()` in their loops. Each of these states must also advance the PPU by the correct number of ticks per CPU tick.

The cleanest approach is a shared helper method (e.g., on `CpuStateFactory` or a new `SimulationTicker`) that advances both together, so no state class can forget to tick the PPU:

```csharp
void TickBoth()
{
    cpu.Tick();
    for (int i = 0; i < PpuCyclesPerCpuCycle; i++)
        ppu.Tick();
}
```

### VBlank notification: decoupling PPU from CPU

The PPU should not hold a direct reference to the CPU. Instead, it exposes a notification mechanism (an event or callback) that fires when VBlank starts. The Backend subscribes to this and calls `cpu.RequestInterrupt()`:

```csharp
ppu.VBlankStarted += () => cpu.RequestInterrupt();
```

This keeps the PPU decoupled: it knows nothing about the CPU, only that "something wants to be notified at VBlank."

### Re-entrancy: why there is no risk

When `ppu.Tick()` fires `VBlankStarted` and the Backend calls `cpu.RequestInterrupt()`, this only **sets a pending flag** on the CPU. The `TickHandler` checks and dispatches that flag only at instruction boundaries — never mid-tick. Since `ppu.Tick()` is called *between* `cpu.Tick()` calls in the co-simulation loop, the CPU is never currently executing when the flag is set. The interrupt is queued, not dispatched immediately. The existing interrupt infrastructure already handles this correctly with no changes needed.
