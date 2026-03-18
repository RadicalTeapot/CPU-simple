# IRQ Vector Table

This document describes the IRQ vector table: what it is, why it exists, and how it is implemented at both the CPU and assembler levels.

---

## What is a vector table?

A **vector table** is a small, fixed-location region in memory that contains *pointers* to interrupt handlers — not the handlers themselves. When an interrupt fires, the CPU reads the handler address from the table at runtime and jumps to it.

This separates two concerns that would otherwise be conflated:

| Without vector table | With vector table |
|---|---|
| Handler must live at a fixed, pre-calculated address | Handler can be placed anywhere in memory |
| CPU jumps directly to the fixed address | CPU reads pointer from table, then jumps |
| Memory reserved for handler is bounded (16 bytes) | Handler size is unlimited |
| Adding more interrupt sources requires new fixed addresses | Adding sources means adding table entries |

---

## Memory layout

### 8-bit mode (256-byte address space)

```
0x00 – 0xE6   Code + data + IRQ handler  (sequential, 231 bytes max)
0xE7          IRQ vector table           (1 byte: handler address)
0xE8 – 0xEF   MMIO                       (8 bytes)
0xF0 – 0xFF   Stack                      (16 bytes)
```

The vector table is a single byte containing the 8-bit address of the IRQ handler.

### 16-bit mode (64 KB address space)

```
0x0000 – 0xEEFD   Code + data + IRQ handler  (sequential)
0xEEFE – 0xEEFF   IRQ vector table           (2 bytes: little-endian handler address)
0xEF00 – 0xEFFF   MMIO                       (256 bytes)
0xFF00 – 0xFFFF   Stack                      (256 bytes)
```

The vector table is two bytes, little-endian (low byte at the lower address).

### Vector table address formula

```
VectorTableAddress = MemorySize - StackSize - MmioRegionSize - VectorTableSize
```

For 8-bit defaults: `256 - 16 - 8 - 1 = 231 = 0xE7`.

This is defined in `CPU/Config.cs` as `IrqVectorTableAddress` and `VectorTableSize`.

---

## CPU-level implementation

### Interrupt dispatch sequence (8-bit)

When a pending interrupt is detected at an instruction boundary, the CPU creates an `InterruptServiceRoutine` pseudo-opcode. It runs the following micro-ticks:

```
Tick 1 (MemoryWrite — PushStatus):
    status = (I << 2) | (C << 1) | Z
    stack.PushByte(status)
    → next: MemoryWrite

Tick 2 (MemoryWrite — PushPC):
    stack.PushByte(state.GetPC())   // return address
    → next: MemoryRead

Tick 3 (MemoryRead — ReadVector):
    handlerAddress = bus.ReadByte(irqVectorTableAddress)
    state.SetInterruptDisableFlag(true)
    state.SetPC(handlerAddress)
    → Done
```

In 16-bit mode there are five ticks: PushStatus, PushPCHigh, PushPCLow, ReadVectorLow, ReadVectorHigh.

The key addition over a fixed-address jump is **Tick 3**: a bus read from the vector table address. The address read from the table becomes the new PC. This is the indirection that allows the handler to live anywhere.

### Relevant files

| File | Role |
|---|---|
| `CPU/Config.cs` | `IrqVectorTableAddress`, `VectorTableSize` constants |
| `CPU/microcode/TickHandler.cs` | Detects pending interrupt, creates `InterruptServiceRoutine` |
| `CPU/microcode/InterruptServiceRoutine.cs` | Pushes state, reads vector table, sets PC |

---

## Assembler-level implementation

The assembler's `.irq` directive creates an IRQ section. With a vector table, the section is placed **sequentially** in memory alongside `.text` and `.data` — there is no fixed placement constraint on the handler itself.

After all sections are laid out, the assembler emits:

1. **Fill gap**: zero-fill from the end of the last section to the vector table address.
2. **Vector table entry**: 1 byte (8-bit) or 2 bytes (16-bit, little-endian) containing the start address of the `.irq` section, resolved during the second analysis pass.

This is done by `IrqVectorTableEmitNode` (`Assembler/Analysis/EmitNode/IrqVectorTableEmitNode.cs`).

### Example

```asm
.text
    CLI            ; enable interrupts
loop:
    NOP
    JMP [loop]

.irq
handler:           ; placed sequentially after .text — address resolved by assembler
    ; ... handle event ...
    RTI
```

Assembled output (8-bit, abbreviated):

```
0x00   CLI
0x01   NOP         ← loop
0x02   JMP [0x01]
...
0x05   <handler code>   ← .irq section, placed sequentially at 0x05
...
0xE6   0x00 (fill)
0xE7   0x05        ← vector table: points to handler at 0x05
```

### Relevant files

| File | Role |
|---|---|
| `Assembler/Analyser.cs` | Places all sections sequentially; emits fill + vector table at end |
| `Assembler/Analysis/EmitNode/IrqVectorTableEmitNode.cs` | Emits 1 or 2 bytes: the resolved handler address |

---

## Key invariants

| Invariant | Where enforced |
|---|---|
| Vector table address is consistent between CPU and assembler | Both derive from `Config.IrqVectorTableAddress` |
| Handler address is resolved before vector table is emitted | Assembler second pass places all sections before emitting `IrqVectorTableEmitNode` |
| Vector table read is a proper bus tick | `InterruptServiceRoutine.ReadVector()` uses `bus.ReadByte()`, classified as `MemoryRead` |
| Handler size is unconstrained | `.irq` section is placed sequentially; only the pointer is fixed |
