# Interrupt System

This document describes how hardware interrupts work in CPU-simple: how they are requested, how the CPU services them, and how user code enables, disables, and returns from them.

---

## Concepts

### The Interrupt Disable flag (I)

The CPU has a dedicated **Interrupt Disable flag** (`I`) stored in `State`. When `I = true`, pending interrupts are held and not serviced until `I` is cleared. When `I = false`, the CPU will service a pending interrupt at the next instruction boundary.

- Cleared (`I = false`) on CPU reset — interrupts are **enabled** by default
- Set automatically on interrupt entry — prevents nested interrupts
- Restored by `RTI` to whatever value it held before the interrupt fired

### Instructions

| Mnemonic | Opcode | Effect |
|----------|--------|--------|
| `SEI` | `0x06` | Set I flag — **disable** interrupts |
| `CLI` | `0x07` | Clear I flag — **enable** interrupts |
| `RTI` | `0x09` | Return from interrupt — restore PC and flags from stack |

### The IRQ vector address

The CPU has a fixed **IRQ vector address** where the interrupt handler must be placed. In 8-bit mode with default config (`256` bytes total, `16`-byte stack):

```
IRQ vector address = MemorySize - StackSize - IrqSectionSize
                   = 256 - 16 - 16 = 224 = 0xE0
```

The assembler's `.irq` section directive automatically places code at this address, padding the gap from the end of regular code with zero bytes.

> **NOTE**: The current design uses a single fixed vector address. A future upgrade could replace this with a vector table, allowing different handler addresses for different interrupt sources.

**Memory layout (8-bit default):**

```
0x00 – 0xDF   Code + data (224 bytes)
0xE0 – 0xEF   IRQ handler section (16 bytes, IrqSectionSize)
0xF0 – 0xFF   Stack (16 bytes, StackSize)
```

---

## Requesting an interrupt

External hardware (e.g., a PPU) calls `CPU.RequestInterrupt()`. This sets an internal `_pendingInterrupt` flag inside `TickHandler`.

```csharp
// External hardware / backend
cpu.RequestInterrupt();
```

The flag stays set until the CPU services the interrupt. If `I` is set when `RequestInterrupt()` is called, the interrupt simply remains pending — it is not lost.

---

## Interrupt service sequence

### When does service happen?

Interrupts are serviced **only at instruction boundaries** — specifically at the `FetchOpcode` phase, before the next instruction is fetched. This guarantees that the current instruction always completes atomically.

The check in `TickHandler.FetchCurrentInstruction()`:

```csharp
if (_pendingInterrupt && !_state.I)
{
    JumpToInterrupt();
    return;
}
// ... normal fetch
```

If the interrupt is pending but `I` is set, the fetch proceeds normally and the interrupt stays pending. It will be checked again at the next instruction boundary.

### JumpToInterrupt

When the condition is met, `JumpToInterrupt()` clears `_pendingInterrupt`, instantiates an `InterruptServiceRoutine` opcode, and hands control to it — the ISR runs in place of the would-be next instruction fetch.

```csharp
private void JumpToInterrupt()
{
    _pendingInterrupt = false;
    _phaseCount = 0;
    _currentBaseCode = OpcodeBaseCode.NOP;
    _currentOpcode = new InterruptServiceRoutine(_state, _stack, _irqVectorAddress);
    _currentPhase = _currentOpcode.GetStartPhaseType(); // MemoryWrite
}
```

The `InterruptServiceRoutine` is an internal class — it has no `[Opcode]` attribute and is never created by `OpcodeFactory`. It exists solely to push state to the stack and redirect the PC.

### What the ISR does (8-bit mode)

The ISR executes as two `MemoryWrite` ticks:

```
Tick 1 (FetchOpcode phase detects interrupt):
    → ISR created, _currentPhase = MemoryWrite

Tick 2 (MemoryWrite — PushStatus):
    status = (I << 2) | (C << 1) | Z
    stack.PushByte(status)
    → next: MemoryWrite

Tick 3 (MemoryWrite — PushPC):
    stack.PushByte(state.GetPC())   // PC of the *next* instruction to run
    state.SetInterruptDisableFlag(true)
    state.SetPC(irqVectorAddress)
    → Done
```

In 16-bit mode there are three `MemoryWrite` ticks: PushStatus, PushPCHigh, PushPCLow.

**Status byte encoding:**

```
bit 2 = I (interrupt disable)
bit 1 = C (carry)
bit 0 = Z (zero)
```

This packing mirrors the approach used by real processors (e.g., the 6502's P register) and lets a single byte capture the full flag state needed to resume.

**The PC value pushed** is the address of the instruction that *would have* been fetched next — i.e., the return address for `RTI`. Because the ISR fires at the `FetchOpcode` phase (before PC is incremented for the next instruction), the PC on the stack correctly points at the interrupted program's next instruction.

**Setting I on entry** prevents the handler itself from being interrupted immediately. The handler must explicitly execute `CLI` to re-enable interrupts (if it wants to allow nesting), or leave `I` set until `RTI` restores the original value.

---

## Returning from an interrupt (RTI)

`RTI` reverses the ISR sequence. It pops in the opposite order — PC first, then the status byte:

**8-bit mode (3 ticks total):**

```
Tick 1 (FetchOpcode): fetch RTI byte, PC++

Tick 2 (MemoryRead — PopPC):
    PC = stack.PopByte()           // restore return address
    → next: MemoryRead

Tick 3 (MemoryRead — PopStatus):
    status = stack.PopByte()
    Z = (status & 0x01) != 0
    C = (status & 0x02) != 0
    I = (status & 0x04) != 0      // restores the I flag as it was before the interrupt
    → Done
```

The I flag restored by `RTI` is whatever was saved before the ISR entry. If `I` was clear before the interrupt (as it normally is), `RTI` re-enables interrupts automatically.

---

## Using interrupts in assembly

### Minimal interrupt handler

```asm
.text
    CLI            ; enable interrupts
    ; ... main program ...
    HLT

.irq
handler:
    ; ... handle interrupt ...
    RTI            ; return and restore I, C, Z
```

The `.irq` section is automatically placed at `0xE0` by the assembler. The CPU jumps there when an interrupt fires.

### Temporarily disabling interrupts

```asm
    SEI            ; disable interrupts (I = 1)
    ; ... critical section (interrupt-safe) ...
    CLI            ; re-enable (I = 0)
```

### Interrupt-driven loop

```asm
.text
start:
    CLI            ; enable interrupts
loop:
    NOP            ; main loop doing nothing
    JMP [loop]

.irq
handler:
    ; handle event
    RTI
```

---

## Key invariants

| Invariant | Where enforced |
|-----------|---------------|
| Interrupts only service at instruction boundaries | `TickHandler.FetchCurrentInstruction()` |
| Pending interrupt held while I = 1 | `if (_pendingInterrupt && !_state.I)` check |
| I set automatically on interrupt entry | `InterruptServiceRoutine.PushPC()` |
| Flags fully restored by RTI (including I) | `RTI.PopStatus()` unpacks all three bits |
| IRQ vector address consistent between CPU and assembler | Both derive from `Config.IrqSectionSize = 16` |

---

## Component map

| Component | File | Role |
|-----------|------|------|
| `State.I` | `CPU/components/State.cs` | Flag storage + get/set methods |
| `TickHandler` | `CPU/microcode/TickHandler.cs` | Checks `_pendingInterrupt && !I` at fetch; calls `JumpToInterrupt()` |
| `InterruptServiceRoutine` | `CPU/microcode/InterruptServiceRoutine.cs` | Internal opcode that pushes status+PC and jumps to vector |
| `CPU.RequestInterrupt()` | `CPU/CPU.cs` | Public API for external hardware to signal an interrupt |
| `Config.IrqSectionSize` | `CPU/Config.cs` | Shared constant (16) used by both CPU and assembler |
| `Config.IrqVectorAddress` | `CPU/Config.cs` | Computed: `MemorySize - StackSize - IrqSectionSize` |
| `SEI` / `CLIOpcode` | `CPU/opcodes/SEI.cs`, `CLIOpcode.cs` | Set/clear the I flag |
| `RTI` | `CPU/opcodes/RTI.cs` | Pops PC then status byte, restores all three flags |
| `Section.Type.Irq` | `Assembler/Analysis/Section.cs` | Assembler section type for IRQ code |
| `Analyser` `.irq` handling | `Assembler/Analyser.cs` | Fixed-address placement with fill gap |
