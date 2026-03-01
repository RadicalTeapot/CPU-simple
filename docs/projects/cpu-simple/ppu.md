# PPU (Picture Processing Unit)

This document describes the architecture of the PPU: its relationship with the CPU, how data flows in and out, and how it produces pixels on screen. It is the authoritative reference for PPU design decisions in this project.

---

## Overview

The PPU is a co-processor dedicated to graphics. It has its own **VRAM** (Video RAM), its own internal bus, and runs its own rendering pipeline independently of the CPU. The CPU communicates with the PPU through a small set of **memory-mapped I/O registers** — the only window into the PPU from the CPU's perspective.

This design preserves the CPU's address space (256 bytes in 8-bit mode, 64KB in 16-bit mode) for program code and data. VRAM can be as large as needed without consuming any CPU address space.

---

## Memory-Mapped I/O (MMIO)

### How it works

The CPU has a single address bus. When an opcode executes `sta r0, [0xF0]`, the bus emits a write to address `0xF0`. Something must decide: does this go to main RAM, or to a PPU register?

This decision is made by a **bus decoder** (also called a memory mapper or MMIO controller) that sits between the CPU and all addressable devices. It inspects the address of every read and write and routes it to the correct target:

```
CPU
 │
 ▼
Bus Decoder  ──── 0x00–0xEF ──▶ main RAM          (8-bit mode)
                ── 0xF0–0xFF ──▶ PPU registers

Bus Decoder  ──── 0x0000–0xFEFF ──▶ main RAM      (16-bit mode)
                ── 0xFF00–0xFFFF ──▶ PPU registers
```

The decoder intercepts **both reads and writes**. Some PPU registers are readable (e.g., PPUSTATUS reports whether VBlank is active or sprite overflow has occurred). The decoder handles both directions.

### Implementation note

`CPU/components/BusDecoder.cs` implements `IBus` and sits between the CPU and all addressable devices. It holds references to both `Memory` and the `IMmioDevice` (PPU registers), forwarding calls based on address range. `IMmioDevice.ReadRegister`/`WriteRegister` receive byte offsets (base address already subtracted).

The stack is **not** part of this routing. Stack operations (`Push`/`Pop`) bypass the address bus entirely.

### MMIO register map

`IMmioDevice` receives byte offsets from the base address (0xF0 in 8-bit mode, 0xFF00 in 16-bit mode). The register layout is identical in both builds; 16-bit mode adds three backdrop color registers that have no 8-bit counterpart.

| Offset | Name        | R/W | Description |
|--------|-------------|-----|-------------|
| 0      | `PPUADDR`   | W   | VRAM address target (two-write latch: high byte first, then low byte) |
| 1      | `PPUDATA`   | W   | Write one byte to VRAM at the current address; auto-increments the address |
| 2      | `PPUSTATUS` | R   | Bit 7 = VBlank active; bit 6 = sprite overflow. Reading clears bit 7 and resets the PPUADDR latch. |
| 3      | `PPUBDR`    | W   | Backdrop red component (16-bit build only) |
| 4      | `PPUBDG`    | W   | Backdrop green component (16-bit build only) |
| 5      | `PPUBDB`    | W   | Backdrop blue component (16-bit build only) |

In 8-bit mode (16 MMIO slots at 0xF0–0xFF), offsets 0–2 are used; 0xF3–0xFF are reserved.

---

## Writing Data to VRAM: the Address/Data Register Pair

The CPU cannot address VRAM directly. To write bulk data (tile patterns, colormaps, tilemaps, OAM) into VRAM, the PPU exposes two registers:

| Register  | Purpose |
|-----------|---------|
| `PPUADDR` | Sets the target address in VRAM (two-write latch, see below) |
| `PPUDATA` | Writes one byte to VRAM at the current address, then auto-increments |

The CPU upload loop looks like:

```
; Set VRAM destination address (two writes: high byte, then low byte)
ldi r0, #0x08          ; high byte of tilemap base (0x0880 in 8-bit build)
sta r0, [PPUADDR]
ldi r0, #0x80          ; low byte
sta r0, [PPUADDR]

; Write tilemap bytes one at a time
ldi r1, #TILE_BYTE_COUNT
ldi r2, #tile_data_start
upload_loop:
  lda r3, [r2]         ; load next byte from CPU RAM
  sta r3, [PPUDATA]    ; write to VRAM (auto-increments PPUADDR)
  inc r2
  dec r1
  jnz [upload_loop]
```

After each `PPUDATA` write the PPU increments its internal VRAM pointer. To jump to a non-contiguous region, write a new address to `PPUADDR`.

### PPUADDR write latch

Both builds use VRAM larger than 256 bytes, so a single 8-bit `PPUADDR` write cannot address all of it. The PPU uses a **two-write latch**: `PPUADDR` requires two consecutive writes — **high byte first, then low byte** — to set the full address. An internal 1-bit toggle tracks which byte is expected next.

The latch resets to "expecting high byte" when:
- The second (low) byte is written (normal completion), or
- `PPUSTATUS` is read (allows recovery from a partially-written address after an interrupt)

### CHR ROM preload (8-bit build only)

In the 8-bit build, VRAM is initialized at PPU construction time by copying a hardcoded C# byte array (the CHR ROM) into the CHR region `[0x0000, ChrBase + ChrSize)`. This simulates a tile library ROM loaded at power-on.

Programmers can partially overwrite the library at runtime by writing to `PPUADDR`/`PPUDATA` at any tile's base offset (`tile_index × BytesPerTile`). Only the written tiles change; the rest of the ROM-initialized data persists until overwritten.

The 16-bit build starts with CHR VRAM zeroed. The programmer is responsible for uploading all tile data before rendering.

### VBlank is a budget, not unlimited time

All VRAM uploads must happen during **VBlank**. VBlank lasts a fixed number of CPU cycles (see Clocking). If more data needs to be uploaded than fits in one VBlank window, the upload must be spread across multiple frames.

---

## Tile-Based Rendering

### Why not a framebuffer?

A raw framebuffer at 1 byte per pixel requires:

```
128 × 120 × 1 byte = 15,360 bytes   (8-bit build)
256 × 240 × 1 byte = 61,440 bytes   (16-bit build)
```

Both exceed the CPU address space in their respective modes. Uploading even 15KB per frame through the narrow PPUDATA register would exhaust the VBlank window. Framebuffers are not viable.

### Tiles

A **tile** is a fixed-size block of pixel data — always 8×8 pixels in this PPU. Rather than storing pixel colors directly, each pixel stores a **colormap index** — an index into a small color lookup table. The bit depth determines how many colors a tile can reference:

```
8×8 pixels × 2 bits/pixel = 128 bits = 16 bytes per tile   (8-bit build, 2bpp, 4 colors)
8×8 pixels × 4 bits/pixel = 256 bits = 32 bytes per tile   (16-bit build, 4bpp, 16 colors)
```

The full tile library:

```
136 tiles × 16 bytes = 2,176 bytes   (8-bit build)
256 tiles × 32 bytes = 8,192 bytes   (16-bit build)
```

### The tilemap

The tile pattern data alone does not describe what the screen looks like. The PPU also needs to know *which tile goes where*. This is stored in the **tilemap**: a 2D grid of tile indices, one byte per cell.

```
16 × 15 =  240 bytes   (8-bit build, 128×120 px ÷ 8 = 16×15 tiles)
32 × 30 =  960 bytes   (16-bit build, 256×240 px ÷ 8 = 32×30 tiles)
```

Each byte is an index into the tile library. The full background layout costs under 1 KB.

### Tile reuse

The same tile pattern can appear at any number of tilemap positions. A brick-wall background might consist of 200 tilemap entries all referencing the same 16-byte pattern. Reuse is free — the tilemap just repeats the index.

### Attribute map

Each tile position has a **colormap index** stored in the attribute map. Because the number of colormaps is small (4 or 16), this index is packed at sub-byte granularity:

```
240 tiles × 2 bits = 480 bits = 60 bytes   (8-bit build, 4 colormaps → 2 bits/tile)
960 tiles × 4 bits = 3840 bits = 480 bytes (16-bit build, 16 colormaps → 4 bits/tile)
```

**8-bit packing:** each byte holds 4 entries. Tile index N occupies bits `[7 − 2×(N%4) : 6 − 2×(N%4)]` of byte `N/4` (MSB-first within the byte).

**16-bit packing:** each byte holds 2 entries. Even tile indices occupy the high nibble; odd tile indices occupy the low nibble of byte `N/2`.

---

## Sprites and OAM

### Sprites vs tiles

A **tile** is a graphical pattern stored in VRAM (CHR data). A **sprite** is a screen object that references a tile pattern and adds position and display attributes. Multiple sprites can reference the same tile pattern.

### OAM (Object Attribute Memory)

Sprite instances are stored in an **OAM** region within VRAM (see VRAM Layout). OAM formats differ between builds:

**8-bit OAM — 3 bytes per entry, 16 entries total:**

| Byte | Field | Encoding |
|------|-------|----------|
| 0 | Position | bits [7:4] = Y tile (0–15), bits [3:0] = X tile (0–15) |
| 1 | CHR index | Tile index into the library (0–135) |
| 2 | Attributes | See below |

**16-bit OAM — 4 bytes per entry, 64 entries total:**

| Byte | Field | Encoding |
|------|-------|----------|
| 0 | Y position | Pixel row (0–255; values ≥ 240 are off-screen) |
| 1 | X position | Pixel column (0–255) |
| 2 | CHR index | Tile index into the library (0–255) |
| 3 | Attributes | See below |

**Attributes byte:**

| Bits | 8-bit meaning | 16-bit meaning |
|------|---------------|----------------|
| [1:0] / [3:0] | Colormap index (2-bit, 0–3) | Colormap index (4-bit, 0–15) |
| [4] | Unused (must be 0) | Unused (must be 0) |
| [5] | Priority: 0 = in front of BG, 1 = behind BG | Same |
| [6] | Horizontal flip | Same |
| [7] | Vertical flip | Same |

### Sprite positioning

**8-bit sprites are tile-aligned.** The Y/X position encodes a tile grid coordinate, not a pixel coordinate. Sprites snap to the 8-pixel grid. This is intentional: with only 4 bits per axis and a 16×15 tile grid, pixel-level positioning would not fit in 1 byte.

**16-bit sprites are pixel-aligned.** Each axis gets a full byte, giving sub-tile precision across the 256×240 display.

### Flip flags

Horizontal and vertical flip bits allow a single tile pattern to serve up to 4 orientations without storing additional CHR data. A character walking left is the walking-right tile flipped horizontally.

### Hiding sprites

Sprites have no dedicated visibility bit. To hide a sprite:
- **8-bit:** set Y tile = `0xF` (tile row 15, one row below the bottom of the 15-row screen)
- **16-bit:** set Y pixel = `0xFF` or any value ≥ 240

### Sprite priority between sprites

When two sprites overlap, the sprite with the **lower OAM index wins**. This is a hardware convention, not a per-entry field.

---

## Colors

### Color encoding

All colors in this PPU are stored as **direct RGB triplets**: one byte each for red, green, and blue, in that order. There is no master palette; the RGB values are used directly as output color.

### Colormaps

A **colormap** is a small array of RGB colors. Tiles and sprites reference colors through their colormap index (a 2-bit or 4-bit pixel value). Each tile position is assigned one colormap via the attribute map; each sprite carries its colormap index in the attributes byte.

Color index **0 is always transparent** for both backgrounds and sprites — the PPU skips rendering that pixel and falls through to the priority resolution below.

| Property | 8-bit | 16-bit |
|----------|-------|--------|
| Number of colormaps | 4 | 16 |
| Usable colors per colormap | 3 (indices 1–3) | 15 (indices 1–15) |
| Bytes per colormap | 3 colors × 3 bytes = 9 bytes | 15 colors × 3 bytes = 45 bytes |
| Total colormap VRAM | 4 × 9 = 36 bytes | 16 × 45 = 720 bytes |

Colormaps are stored sequentially in VRAM. Within a colormap, colors are stored in index order starting at index 1 (index 0 has no stored data — it is always transparent). Color `c` of colormap `m` is at:

```
colormap_base + m × (ColorsPerColormap × 3) + (c − 1) × 3
```

### Backdrop color

When a pixel has no opaque background tile and no opaque sprite in front, the **backdrop color** is rendered:

- **8-bit:** the backdrop is colormap 0, color index 1 (the first stored entry of the first colormap). No extra register is needed; the programmer controls the backdrop by writing the desired RGB value to that colormap entry.
- **16-bit:** a dedicated global backdrop color is stored in the `PPUBDR`/`PPUBDG`/`PPUBDB` registers. It is independent of any colormap entry and is not subject to the transparency rule.

---

## Rendering Pipeline: Scanlines

The PPU renders one **scanline** at a time, from top to bottom, left to right.

### Frame structure

```
                    8-bit build        16-bit build
                    ──────────         ────────────
Active rendering    scanlines 0–119    scanlines 0–239
Post-render (idle)  scanline 120       scanline 240
VBlank              scanlines 121–135  scanlines 241–260
Pre-render          scanline 136       scanline 261
─────────────────────────────────────────────────────
Total               137 scanlines      262 scanlines
Cycles/scanline     171                341
```

These defaults are configurable via `PpuConfig` (see Configuration). The clock ratio between PPU and CPU ticks is TBD and lives in `PpuConfig.PpuCyclesPerCpuCycle`.

### HBlank: the gap between scanlines

At the end of each visible scanline there is a **horizontal blank (HBlank)** period during which the PPU is not outputting pixels. This is used for:
- **Sprite evaluation** for the *next* scanline
- **Tile prefetch** for the first tiles of the next scanline

Sprite evaluation happens during the HBlank of the *previous* scanline, so the active-sprite list is ready before pixel output begins.

### Per-scanline sequence

1. **Sprite evaluation (during previous scanline's HBlank)** — The PPU scans all OAM entries and builds a short list of sprites whose Y range overlaps the upcoming scanline. Capped at **8 sprites per scanline** in both builds; exceeding the cap sets the sprite-overflow bit in PPUSTATUS.

2. **Pipelined tile fetch (interleaved with pixel output)** — As the PPU outputs pixels for tile N, it simultaneously fetches the CHR row and tilemap index for tile N+1. Every 8 pixels the pipeline shifts. The PPU is never idle during active rendering.

3. **Pixel output** — For each pixel position:
   - **Background pixel:** tilemap index → CHR row → colormap index → color. If colormap index is 0 (transparent), skip.
   - **Sprite pixel:** check the evaluated sprite list for any sprite covering this X position → CHR row → colormap index → color. If colormap index is 0 (transparent), skip.
   - **Priority resolution:**
     - Sprite colormap index is opaque AND sprite priority = "in front" → sprite color wins.
     - Sprite colormap index is opaque AND sprite priority = "behind" AND background is also opaque → background wins.
     - Sprite-vs-sprite: lower OAM index wins.
     - No opaque pixel → backdrop color.
   - **Output:** final RGB value sent to the display.

### VBlank

After the last visible scanline (and the post-render idle line), the PPU enters **VBlank** — the only safe window for the CPU to update PPU state.

At the start of VBlank:
1. The PPU sets the VBlank flag in PPUSTATUS.
2. The PPU fires the `VBlankStarted` event; the Backend calls `cpu.RequestInterrupt()`.
3. The CPU's ISR runs: uploads tile data, updates the tilemap, rewrites OAM.
4. VBlank ends; the PPU clears the VBlank flag and resets to scanline 0.

### Mid-frame register writes

Writes to PPU control registers (backdrop color, colormap entries) take effect at the **next scanline boundary**, not mid-pixel. Deliberate mid-frame writes to the backdrop or colormap registers produce raster effects visible only on scanlines rendered after the write.

---

## 8-bit vs 16-bit Summary

| Property | 8-bit | 16-bit |
|---|---|---|
| Resolution | 128 × 120 px | 256 × 240 px |
| Tile grid | 16 × 15 | 32 × 30 |
| Tile format | 8×8, 2bpp | 8×8, 4bpp |
| Bytes per tile | 16 | 32 |
| Tile library size | 136 tiles | 256 tiles |
| CHR at startup | Preloaded from C# ROM array | Zeroed (programmer uploads) |
| Colormaps | 4 | 16 |
| Colors per colormap | 3 usable + transparent | 15 usable + transparent |
| Attribute map granularity | Per tile, 2 bits | Per tile, 4 bits |
| Sprite positioning | Tile-aligned (8px grid) | Pixel-aligned |
| OAM entries | 16 | 64 |
| Bytes per OAM entry | 3 | 4 |
| Max sprites per scanline | 8 | 8 |
| Backdrop color | Colormap 0, entry 1 (no register) | PPUBDR/G/B registers |
| VRAM size | 2,560 bytes (2.5 KB) | 10,608 bytes (~10.4 KB) |
| PPUADDR bits needed | 12 | 14 |
| PPUADDR latch | Two-write (high then low) | Two-write (high then low) |
| MMIO registers used | 3 (offsets 0–2) | 6 (offsets 0–5) |

---

## VRAM Layout

VRAM is divided into five consecutive regions. Regions are tightly packed with no padding.

### 8-bit build (2,560 bytes = 0x0A00)

| Region | Start | End | Size | Description |
|--------|-------|-----|------|-------------|
| CHR data | `0x0000` | `0x087F` | 2,176 B | 136 tiles × 16 bytes |
| Tilemap | `0x0880` | `0x096F` | 240 B | 16×15 tile indices |
| Attribute map | `0x0970` | `0x09AB` | 60 B | 240 tiles × 2 bits, packed |
| Colormaps | `0x09AC` | `0x09CF` | 36 B | 4 colormaps × 3 colors × 3 bytes RGB |
| OAM | `0x09D0` | `0x09FF` | 48 B | 16 sprites × 3 bytes |

### 16-bit build (10,608 bytes = 0x2970)

| Region | Start | End | Size | Description |
|--------|-------|-----|------|-------------|
| CHR data | `0x0000` | `0x1FFF` | 8,192 B | 256 tiles × 32 bytes |
| Tilemap | `0x2000` | `0x23BF` | 960 B | 32×30 tile indices |
| Attribute map | `0x23C0` | `0x259F` | 480 B | 960 tiles × 4 bits, packed |
| Colormaps | `0x25A0` | `0x286F` | 720 B | 16 colormaps × 15 colors × 3 bytes RGB |
| OAM | `0x2870` | `0x296F` | 256 B | 64 sprites × 4 bytes |

### Useful derived addresses

To address tile `N` in CHR: `N × BytesPerTile` (offset from VRAM base).
To address tilemap cell `(col, row)`: `TilemapBase + row × TilemapWidth + col`.
To address OAM entry `N`: `OamBase + N × BytesPerSprite`.
Colormap `M`, color `C` (1-based): `ColormapBase + M × (ColorsPerColormap × 3) + (C − 1) × 3`.

---

## Configuration

### `PpuConfig` struct

`PpuConfig` lives in the `PPU` project and drives all size and timing decisions. The `#if x16` factory methods provide build-correct defaults; the clock ratio (`PpuCyclesPerCpuCycle`) must be set at runtime once the target frequency ratio is decided.

```csharp
public readonly struct PpuConfig(
    int screenWidth, int screenHeight,
    int bitsPerPixel,
    int tileCount,
    int colormapCount, int colorsPerColormap,
    int spriteCount, int bytesPerSprite,
    int maxSpritesPerScanline,
    int cyclesPerScanline, int vblankStartScanline, int totalScanlines,
    int ppuCyclesPerCpuCycle)
{
    public const int TilePixelSize = 8;

    // Display
    public int ScreenWidth { get; } = screenWidth;
    public int ScreenHeight { get; } = screenHeight;
    public int TilemapWidth { get; } = screenWidth / TilePixelSize;
    public int TilemapHeight { get; } = screenHeight / TilePixelSize;

    // Tile format
    public int BitsPerPixel { get; } = bitsPerPixel;
    public int BytesPerTile { get; } = TilePixelSize * TilePixelSize * bitsPerPixel / 8;
    public int TileCount { get; } = tileCount;

    // Color / palette
    public int ColormapCount { get; } = colormapCount;
    public int ColorsPerColormap { get; } = colorsPerColormap; // excludes transparent index 0
    public int ColormapBits { get; } = colormapCount <= 4 ? 2 : 4; // bits used in attribute map

    // Sprites
    public int SpriteCount { get; } = spriteCount;
    public int BytesPerSprite { get; } = bytesPerSprite;
    public int MaxSpritesPerScanline { get; } = maxSpritesPerScanline;

    // Scanline timing
    public int CyclesPerScanline { get; } = cyclesPerScanline;
    public int VBlankStartScanline { get; } = vblankStartScanline;
    public int TotalScanlines { get; } = totalScanlines;

    // Co-simulation
    public int PpuCyclesPerCpuCycle { get; } = ppuCyclesPerCpuCycle; // 0 = not yet set

    // Derived layout
    public PpuVramLayout Layout => new(this);

#if x16
    public static PpuConfig Default => new(
        screenWidth: 256, screenHeight: 240,
        bitsPerPixel: 4, tileCount: 256,
        colormapCount: 16, colorsPerColormap: 15,
        spriteCount: 64, bytesPerSprite: 4,
        maxSpritesPerScanline: 8,
        cyclesPerScanline: 341, vblankStartScanline: 241, totalScanlines: 262,
        ppuCyclesPerCpuCycle: 0);
#else
    public static PpuConfig Default => new(
        screenWidth: 128, screenHeight: 120,
        bitsPerPixel: 2, tileCount: 136,
        colormapCount: 4, colorsPerColormap: 3,
        spriteCount: 16, bytesPerSprite: 3,
        maxSpritesPerScanline: 8,
        cyclesPerScanline: 171, vblankStartScanline: 121, totalScanlines: 137,
        ppuCyclesPerCpuCycle: 0);
#endif
}
```

### `PpuVramLayout` struct

`PpuVramLayout` derives all VRAM region offsets from a `PpuConfig`. All addresses are byte offsets from the start of VRAM.

```csharp
public readonly struct PpuVramLayout(PpuConfig config)
{
    public int ChrBase { get; } = 0;
    public int ChrSize { get; } = config.TileCount * config.BytesPerTile;

    public int TilemapBase { get; } = config.TileCount * config.BytesPerTile;
    public int TilemapSize { get; } = config.TilemapWidth * config.TilemapHeight;

    public int AttrmapBase { get; } = config.TileCount * config.BytesPerTile
                                    + config.TilemapWidth * config.TilemapHeight;
    public int AttrmapSize { get; } = config.TilemapWidth * config.TilemapHeight
                                    * config.ColormapBits / 8;

    public int ColormapBase { get; } = config.TileCount * config.BytesPerTile
                                     + config.TilemapWidth * config.TilemapHeight
                                     + config.TilemapWidth * config.TilemapHeight * config.ColormapBits / 8;
    public int ColormapSize { get; } = config.ColormapCount * config.ColorsPerColormap * 3;

    public int OamBase { get; } = config.TileCount * config.BytesPerTile
                                + config.TilemapWidth * config.TilemapHeight
                                + config.TilemapWidth * config.TilemapHeight * config.ColormapBits / 8
                                + config.ColormapCount * config.ColorsPerColormap * 3;
    public int OamSize { get; } = config.SpriteCount * config.BytesPerSprite;

    public int TotalSize { get; } = config.TileCount * config.BytesPerTile
                                  + config.TilemapWidth * config.TilemapHeight
                                  + config.TilemapWidth * config.TilemapHeight * config.ColormapBits / 8
                                  + config.ColormapCount * config.ColorsPerColormap * 3
                                  + config.SpriteCount * config.BytesPerSprite;
}
```

---

## Clocking and Synchronization

### Independent clocks

The CPU and PPU run at independent clock rates. In real hardware these rates are fixed multiples of each other (the NES PPU runs at 3× the CPU clock). This ratio determines:
- How many PPU ticks occur per CPU tick
- How many CPU cycles fit inside one VBlank window
- How precisely mid-frame timing effects can be controlled

`PpuConfig.PpuCyclesPerCpuCycle` holds this ratio. It must be set before the simulation starts and is documented alongside other simulation parameters in the Backend. The 16-bit build default of 341 cycles per scanline / 262 scanlines follows NES proportions; the 8-bit build default (171/137) scales proportionally for the smaller display.

### Simpler alternative: single-threaded co-simulation

Rather than two OS threads, the simulation uses a **single-threaded co-simulation loop**:

```csharp
void TickBoth()
{
    cpu.Tick();
    for (int i = 0; i < config.PpuCyclesPerCpuCycle; i++)
        ppu.Tick();
}
```

There is no thread concurrency, so no data races and no synchronization primitives needed. VBlank is detected during `ppu.Tick()` and fires `VBlankStarted`, which the Backend wires to `cpu.RequestInterrupt()`.

### VBlank interrupt: notification, not synchronization

The interrupt the PPU fires at the start of VBlank is a **notification** — it tells the CPU "VBlank has started, you may now safely update PPU state." It does not synchronize the two clocks. Nothing prevents a buggy program from writing to PPU registers mid-frame; the interrupt is an application-level protocol, not a hardware lock.

### VBlank notification: decoupling PPU from CPU

The PPU does not hold a direct reference to the CPU. It exposes an event that the Backend subscribes to:

```csharp
ppu.VBlankStarted += () => cpu.RequestInterrupt();
```

When `VBlankStarted` fires inside `ppu.Tick()`, `RequestInterrupt()` only sets a pending flag on the CPU. The flag is dispatched only at instruction boundaries — never mid-tick. Since `ppu.Tick()` is called between `cpu.Tick()` calls, there is no re-entrancy risk.

---

## Debugger Integration

### Tick traces: separate streams for CPU and PPU

`TickTrace` records CPU bus transactions — memory reads/writes, stack operations. PPU-internal VRAM accesses (tile fetches, sprite evaluation reads) happen on a completely separate bus that the CPU never sees. Including them in `TickTrace` would conflate two unrelated buses.

PPU internal activity uses its own **PPU trace stream**, parallel to but separate from `TickTrace`.

### Watchpoints

**CPU writes to PPU registers** are already covered by the existing `AddressWatchpoint`. Since MMIO register writes pass through the bus decoder as normal address-space writes, `wp write 0xF0` already triggers on a CPU store to PPUADDR. A convenience alias (`wp ppureg <name>`) expanding to the known register address would be a UX improvement.

**PPU-internal watchpoints** (break on VBlank, HBlank, specific scanline) need a new watchpoint family mirroring `IWatchpoint` / `WatchpointContainer`:

- A `PpuWatchpointContainer` holding `IPpuWatchpoint` instances
- Concrete types: `VBlankWatchpoint`, `ScanlineWatchpoint`, etc.
- Evaluated inside `ppu.Tick()` against a `PpuTickTrace`
- New `watchpoint` sub-commands: `wp ppu vblank`, `wp ppu scanline 120`

### Halting on a PPU watchpoint

When a PPU watchpoint fires inside `ppu.Tick()`, the simulation must stop. The cleanest signal is a return value from `ppu.Tick()` (e.g., a `PpuTickResult` with a `HaltRequested` flag), which `TickBoth()` checks after each PPU tick:

```csharp
void TickBoth()
{
    cpu.Tick();
    for (int i = 0; i < config.PpuCyclesPerCpuCycle; i++)
    {
        var result = ppu.Tick();
        if (result.HaltRequested) { /* transition to IdleState */ return; }
    }
}
```

### Ownership and integration with the Backend state machine

The Backend owns the PPU. The execution states (`SteppingState`, `TickingState`, `RunningState`) must all advance the PPU by the correct number of ticks per CPU tick. A shared helper (e.g., on `CpuStateFactory` or a new `SimulationTicker`) ensures no state class can forget to tick the PPU.
