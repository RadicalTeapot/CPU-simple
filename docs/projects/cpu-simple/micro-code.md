# Microcode and Instruction Timing

## Overview

In the current CPU-simple implementation, all instructions execute with the same cost (single tick for the entire Fetch-Decode-Execute cycle). However, real hardware has different timing characteristics for different instructions based on the operations they perform.

This document describes a realistic tick-level timing model for CPU-simple that reflects actual hardware behavior, where each tick represents a discrete bus transaction or internal operation.

## Tick-Based Execution Model

### Core Principle

**Each tick is EITHER a bus transaction OR an internal ALU/control operation — never both.**

This design choice provides:
- **Clean semantics**: Every tick has a single, well-defined purpose
- **Debugger-friendly traces**: Easy to understand what happened at each step
- **Educational clarity**: Avoids the complexity of simulating dummy bus cycles that real hardware performs for timing consistency

### Tick Types

**Bus Ticks — fetch and memory**

Each bus tick performs one memory transaction and may include simple bookkeeping:

| Phase | Description |
|---|---|
| `FetchOpcode` | Read the instruction opcode byte from PC, PC++ (handled by TickHandler) |
| `FetchOperand` | Read a single-byte instruction operand (immediate value, 8-bit address, or reg+offset encoding), PC++ |
| `FetchOperand16Low` | Read the low byte of a 16-bit address operand, PC++ |
| `FetchOperand16High` | Read the high byte of a 16-bit address operand, PC++ |
| `MemoryRead` | Read data from memory or stack (not an instruction byte) |
| `MemoryWrite` | Write data to memory or stack |

**Internal Ticks**

| Phase | Description |
|---|---|
| `AluOp` | Perform an arithmetic or logical computation (result written to register/flags) |
| `EffectiveAddrComputation` | Compute effective address: EA = base register + immediate offset (indexed addressing) |
| `ValueComposition` | Compose a multi-byte value: `(high << 8) | low` (16-bit address/value assembly) |

### The Boundary: What Requires an Internal Tick?

**✅ Allowed during a bus tick (as "bookkeeping"):**
- `PC++` after each instruction byte fetch
- `SP++` / `SP--` during stack push/pop operations
- Latching data into internal registers (`IR`, `op8`, `op16`, `data`)
- Simple muxing/selection of register sources
- Combinational decode (determining addressing mode, register IDs from opcode bits)
- Writing to any destination register (latching — committed at end of tick)
- Updating flags Z and C (latching — committed at end of tick)
- Writing PC from a fetched or combinationally-selected value (latching — same rules as register writes)
- Conditional branch evaluation (combinational — comparing a flag and selecting a new PC)

**❌ Requires its own internal tick:**
- Any ALU computation that produces an architecturally meaningful value:
  - Arithmetic operations (ADD, SUB, INC, DEC)
  - Logical operations (AND, OR, XOR)
  - Shifts and rotates
  - Comparisons (they update flags)
  - Effective address calculation for indexed addressing (base + offset) — `EffectiveAddrComputation` phase
  - 16-bit value composition in 16-bit mode — `ValueComposition` phase

**Pedagogical rule**: *If an operation requires ALU computation (add, subtract, shift, compare, EA arithmetic), it gets its own internal tick. Latching any value — including writing to destination registers, flags, or PC — is free and can be bundled with any bus tick.*

### Decode Phase

**Cost: 0 ticks (combinational logic)**

Decode happens in parallel with fetch using combinational logic. By the time the last instruction byte is fetched, the instruction has been decoded and internal control signals are ready. This reflects how simple CPUs use dedicated decode circuitry that processes opcode bits during memory access.

Decoded information includes:
- Instruction type (ALU, memory, control flow, etc.)
- Source and destination register IDs
- Addressing mode
- Operation type

## CPU State for Microcode Execution

The microcode model requires tracking additional internal state:

- **IR** (Instruction Register): Holds the fetched opcode byte
- **phase**: Current micro-step within instruction execution (state machine position)
- **decoded fields**: Destination/source register IDs, addressing mode (extracted from IR)
- **op8 / op16**: Latched operand bytes (immediate values, addresses, offsets)
- **EA** (Effective Address): Computed memory address for indexed addressing
- **data**: Most recently read value or value pending write
- **pendingInterrupt**: Latched interrupt flag (interrupts are serviced only at instruction boundaries)

## Instruction Timing Breakdown

The total cost of an instruction is measured in **ticks**. Each tick represents either one bus transaction or one internal operation.

### Fetch Phase Costs

The fetch phase reads instruction bytes from memory via bus transactions:

| Instruction Size | Fetch Ticks | Example |
|-----------------|-------------|---------|
| 1 byte | 1 | `NOP`, `ADD R0, R1`, `PSH R0` |
| 2 bytes (8-bit mode) | 2 | `LDI R0, #0x05`, `JMP [addr]` |
| 3 bytes (16-bit mode) | 3 | `LDA R0, [0x1234]`, `CAL [label]` |

Each fetch tick:
1. Performs a bus read to fetch one instruction byte
2. Increments PC (bookkeeping, happens during the same tick)
3. Latches the byte into IR (first byte) or op8/op16 (subsequent bytes)

### Execute Phase Costs

Execute costs depend on the operation type:

#### Register-only transfers: **0 ticks**
- Register-to-register moves: `MOV Rd, Rs`
- Flag operations: `CLC`, `SEC`, `CLZ`, `SEZ`, `SEI`, `CLI`
- Halt: `HLT`
- No-op: `NOP`
- Immediate loads: `LDI Rd, #imm`

These complete without additional ticks—values are simply latched from one register to another during the decode phase, which is combinational.

**Total cost = Fetch ticks only**

#### Register-only ALU operations: **1 tick (internal)**
Operations that only touch registers (no memory access):
- ALU operations on registers: `ADD Rd, Rs`, `SUB Rd, Rs`, `AND Rd, Rs`, `OR Rd, Rs`, `XOR Rd, Rs`, `CMP Rd, Rs`
- Register shifts/rotates: `LSH Rd`, `RSH Rd`, `LRT Rd`, `RRT Rd`
- Register increment/decrement: `INC Rd`, `DEC Rd`

The immediate byte is fetched in a `FetchOperand` tick, one `AluOp` internal tick follows to perform the computation.

`AluOp` tick performs:
1. Read source register values (latching)
2. Execute ALU computation
3. Write result to destination register (latching)
4. Update flags (Zero, Carry) (latching)

**Total cost = Fetch ticks + 1 internal tick**

#### Immediate ALU operations: **1 tick (internal)**
ALU operations with immediate operands:
- Immediate arithmetic: `ADI Rd, #imm`, `SBI Rd, #imm`
- Immediate logic: `ANI Rd, #imm`, `ORI Rd, #imm`, `XRI Rd, #imm`
- Immediate compare: `CPI Rd, #imm`
- Immediate bit test: `BTI Rd, #imm`

The immediate byte is fetched in a `FetchOperand` tick, one `AluOp` internal tick follows to perform the computation.

`AluOp` tick performs:
1. Read source register values (latching)
2. Execute ALU computation
3. Write result to destination register (latching)
4. Update flags (Zero, Carry) (latching)

**Total cost = Fetch ticks + 1 internal tick**

#### Memory read operations: **1 bus tick (read)**
Operations that read from memory (beyond instruction fetch):
- Memory loads: `LDA Rd, [addr]`, `LDX Rd, [Rs + offset]`
- Memory-based ALU: `ADA Rd, [addr]`, `SBA Rd, [addr]`, `CPA Rd, [addr]`, `ANA Rd, [addr]`, `ORA Rd, [addr]`, `XRA Rd, [addr]`
- Memory bit test: `BTA Rd, [addr]`
- Stack pop: `POP Rd`, `PEK Rd`

Cost breakdown:
1. **Fetch ticks**: Read instruction bytes and address/offset operand(s)
2. **Internal tick (if indexed addressing)**: Compute effective address (EA = base + offset)
3. **Bus tick**: Read from memory; latch result into destination register (or `data` for ALU ops)
4. **Internal tick (ALU ops only)**: Compute ALU result, update flags

Register writebacks and flag updates for non-ALU loads (LDA, POP, PEK) are free latching bundled with the `MemoryRead` tick — no extra internal tick needed.

**Direct addressing (non-ALU) total cost = Fetch ticks + 1 bus read**
**Direct addressing (ALU) total cost = Fetch ticks + 1 bus read + 1 internal**
**Indexed addressing (non-ALU) total cost = Fetch ticks + 1 internal (EA calc) + 1 bus read**
**Indexed addressing (ALU) total cost = Fetch ticks + 1 internal (EA calc) + 1 bus read + 1 internal**

#### Memory write operations: **1 bus tick (write)**
Operations that write to memory:
- Memory stores: `STA Rs, [addr]`, `STX Rs, [Rd + offset]`

Cost breakdown:
1. **Fetch ticks**: Read instruction bytes and address/offset
2. **Internal tick (if indexed addressing)**: Compute effective address (EA = base + offset)
3. **Bus tick**: Write register value to memory

**Direct addressing total cost = Fetch ticks + 1 bus write**
**Indexed addressing total cost = Fetch ticks + 1 internal (EA calc) + 1 bus write**

Register reads happen instantly as part of the bus write operation—no separate internal tick needed.

#### Stack operations

**Push (PSH Rs): 1 bus tick (write)**
1. **Fetch tick**: FetchOpcode, PC++
2. **Bus tick**: Write Rs to mem[SP], SP-- (bookkeeping)

**Total: 2 ticks**

**Pop (POP Rd): 1 bus tick (read)**
1. **Fetch tick**: FetchOpcode, PC++
2. **Bus tick**: Read mem[SP] into Rd (latching), SP++ (bookkeeping)

**Total: 2 ticks**

**Peek (PEK Rd): same as POP but SP unchanged**

**Total: 2 ticks**

#### Control flow operations

**Unconditional jump (JMP [addr]): 0 internal ticks (PC update is latching)**

8-bit mode (2 bytes):
1. FetchOpcode, PC++
2. FetchOperand: addr → PC (latching)

**Total: 2 ticks**

16-bit mode (3 bytes):
1. FetchOpcode, PC++
2. FetchOperand16Low: addr_low, PC++
3. FetchOperand16High: addr_high, PC++
4. ValueComposition: PC ← (addr_high << 8) | addr_low (latching)

**Total: 4 ticks**

**Conditional jump (JCC, JCS, JZC, JZS [addr])**

Same as JMP; checking the condition and conditionally writing PC happens in the `ValueComposition` tick.
The cost is the same whether the branch is taken or not.

**Subroutine call (CAL [addr])**

Pushes return address to stack, then jumps. PC ← addr is free latching bundled with the final push tick.

8-bit mode (2 bytes, 8-bit return address):
1. FetchOpcode, PC++
2. FetchOperand: addr, PC++ (now PC points past the instruction)
3. MemoryWrite: Write PC to mem[SP], SP-- ; PC ← addr (latching)

**Total: 3 ticks**

16-bit mode (3 bytes, 16-bit return address):
1. FetchOpcode, PC++
2. FetchOperand16Low: addr_low, PC++
3. FetchOperand16High: addr_high, PC++
4. ValueComposition: target_addr ← (addr_high << 8) | addr_low
5. MemoryWrite: Write PC_high to mem[SP], SP-- (push high byte first)
6. MemoryWrite: Write PC_low to mem[SP], SP-- ; PC ← target_addr (latching)

**Total: 6 ticks**

**Return from subroutine (RET)**

Pops return address from stack. PC ← popped value is free latching bundled with the final pop tick.

8-bit mode (1 byte, 8-bit return address):
1. FetchOpcode, PC++
2. MemoryRead: Read mem[SP] → PC (latching), SP++ (bookkeeping)

**Total: 2 ticks**

16-bit mode (1 byte, 16-bit return address):
1. FetchOpcode, PC++
2. MemoryRead: Read mem[SP] → PC_low, SP++ (bookkeeping)
3. MemoryRead: Read mem[SP] → PC_high, SP++ (bookkeeping)
4. ValueComposition: PC ← (PC_high << 8) | PC_low (latching)

**Total: 4 ticks**

**Return from interrupt (RTI)**

Pops return address from stack, then pops status byte and restores flags (Z, C, I).

8-bit mode (1 byte):
1. FetchOpcode, PC++
2. MemoryRead: Read mem[SP] → PC (latching), SP++
3. MemoryRead: Read mem[SP] → status byte, SP++ ; restore Z, C, I flags (latching)

**Total: 3 ticks**

16-bit mode (1 byte):
1. FetchOpcode, PC++
2. MemoryRead: Read mem[SP] → PC_low, SP++
3. MemoryRead: Read mem[SP] → PC_high, SP++
4. ValueComposition: PC ← (PC_high << 8) | PC_low
5. MemoryRead: Read mem[SP] → status byte, SP++ ; restore Z, C, I flags (latching)

**Total: 5 ticks**

## Interrupt Service Routine (ISR) Timing

When an interrupt fires at an instruction boundary, the CPU executes an internal ISR sequence before jumping to the IRQ vector address. The ISR is not a user-visible instruction — it is triggered automatically by the hardware.

**Status byte encoding:** `(I << 2) | (C << 1) | Z`

8-bit mode:
1. (Interrupt detected at fetch boundary)
2. MemoryWrite: Push status byte to mem[SP], SP--
3. MemoryWrite: Push PC to mem[SP], SP-- ; set I flag ; PC ← IRQ vector address

**Total: 3 ticks** (FetchOpcode + 2 MemoryWrite)

16-bit mode:
1. (Interrupt detected at fetch boundary)
2. MemoryWrite: Push status byte to mem[SP], SP--
3. MemoryWrite: Push PC_high to mem[SP], SP--
4. MemoryWrite: Push PC_low to mem[SP], SP-- ; set I flag ; PC ← IRQ vector address

**Total: 4 ticks** (FetchOpcode + 3 MemoryWrite)

> **NOTE:** The current implementation uses a single fixed IRQ vector address (placed 16 bytes before the stack region). A future upgrade could introduce a vector table for multiple interrupt sources.

## Complete Timing Tables

### 8-bit Mode Examples

| Instruction | Bytes | Total | Breakdown |
|-------------|-------|-------|-----------|
| `NOP` | 1 | 1 | FetchOpcode only |
| `MOV R0, R1` | 1 | 1 | FetchOpcode (register latch) |
| `ADD R0, R1` | 1 | 2 | FetchOpcode + AluOp |
| `LDI R0, #0x05` | 2 | 2 | FetchOpcode + FetchOperand (imm→Rd) |
| `ADI R0, #0x05` | 2 | 3 | FetchOpcode + FetchOperand + AluOp |
| `LDA R0, [0x0C]` | 2 | 3 | FetchOpcode + FetchOperand (addr) + MemoryRead (data→Rd) |
| `STA R0, [0x0C]` | 2 | 3 | FetchOpcode + FetchOperand (addr) + MemoryWrite |
| `ADA R0, [0x10]` | 2 | 4 | FetchOpcode + FetchOperand (addr) + MemoryRead + AluOp |
| `PSH R0` | 1 | 2 | FetchOpcode + MemoryWrite (stack push) |
| `POP R0` | 1 | 2 | FetchOpcode + MemoryRead (pop→Rd) |
| `JMP [label]` | 2 | 2 | FetchOpcode + FetchOperand (addr→PC) |
| `JZS [label]` | 2 | 2 | FetchOpcode + FetchOperand (addr→PC if Z) |
| `CAL [label]` | 2 | 3 | FetchOpcode + FetchOperand (addr) + MemoryWrite (push+jump) |
| `RET` | 1 | 2 | FetchOpcode + MemoryRead (pop→PC) |
| `SEI` / `CLI` | 1 | 1 | FetchOpcode only (flag latch) |
| `RTI` | 1 | 3 | FetchOpcode + MemoryRead (pop→PC) + MemoryRead (pop→status) |

### 16-bit Mode Examples

Instructions with memory addresses use 3 bytes:

| Instruction | Bytes | Total | Breakdown |
|-------------|-------|-------|-----------|
| `LDA R0, [0x1234]` | 3 | 5 | FetchOpcode + FetchOperand16Low + FetchOperand16High + ValueComposition + MemoryRead (data→Rd) |
| `STA R0, [0x1234]` | 3 | 5 | FetchOpcode + FetchOperand16Low + FetchOperand16High + ValueComposition + MemoryWrite |
| `ADA R0, [0x1234]` | 3 | 6 | FetchOpcode + 2×FetchOperand16 + ValueComposition + MemoryRead + AluOp |
| `JMP [label]` | 3 | 4 | FetchOpcode + FetchOperand16Low + FetchOperand16High + ValueComposition (→PC) |
| `JZS [label]` | 3 | 4 | FetchOpcode + FetchOperand16Low + FetchOperand16High + ValueComposition (→PC if Z) |
| `CAL [label]` | 3 | 6 | FetchOpcode + FetchOperand16Low + FetchOperand16High + ValueComposition + 2×MemoryWrite |
| `RET` | 1 | 4 | FetchOpcode + 2×MemoryRead + ValueComposition |
| `RTI` | 1 | 5 | FetchOpcode + 2×MemoryRead + ValueComposition + MemoryRead (pop→status) |

## Micro-Step Examples

These examples show the tick-by-tick execution using the state machine approach:

### `ADD r0, r1` (register ALU)
```
Phase 0: BUS READ  - FetchOpcode → IR, PC++ (PC: 0x00 → 0x01)
Phase 1: INTERNAL  - r0 ← r0 + r1, update flags → DONE
Total: 2 ticks
```

### `LDI r0, #0x05` (8-bit immediate load)
```
Phase 0: BUS READ  - FetchOpcode → IR, PC++ (PC: 0x00 → 0x01)
Phase 1: BUS READ  - FetchOperand → r0 = 0x05, PC++ (PC: 0x01 → 0x02) → DONE
Total: 2 ticks
```

### `LDA r0, [0x0C]` (8-bit mode memory load)
```
Phase 0: BUS READ  - FetchOpcode → IR, PC++ (PC: 0x00 → 0x01)
Phase 1: BUS READ  - FetchOperand: addr=0x0C, PC++ (PC: 0x01 → 0x02)
Phase 2: BUS READ  - MemoryRead: r0 ← mem[0x0C] → DONE
Total: 3 ticks
```

### `JZS [label]` (8-bit mode conditional jump, absolute address)
```
Phase 0: BUS READ  - FetchOpcode → IR, PC++ (PC: 0x10 → 0x11)
Phase 1: BUS READ  - FetchOperand: address → op8
                     if Z==1: PC ← op8 → DONE
                     else: PC++ → DONE

Total: 2 ticks
```

### `CAL [0x0050]` (8-bit mode subroutine call)
```
Phase 0: BUS READ  - FetchOpcode → IR, PC++ (PC: 0x20 → 0x21)
Phase 1: BUS READ  - FetchOperand: addr=0x50, PC++ (PC: 0x21 → 0x22)
Phase 2: BUS WRITE - MemoryWrite: write PC (0x22) to mem[SP], SP-- ; PC ← 0x50 → DONE
Total: 3 ticks
```

### `LDA r0, [0x1234]` (16-bit mode memory load)
```
Phase 0: BUS READ  - FetchOpcode → IR, PC++ (PC: 0x0100 → 0x0101)
Phase 1: BUS READ  - FetchOperand16Low: addr_low=0x34, PC++ (PC: 0x0101 → 0x0102)
Phase 2: BUS READ  - FetchOperand16High: addr_high=0x12, PC++ (PC: 0x0102 → 0x0103)
Phase 3: INTERNAL  - ValueComposition: EA = (0x12 << 8) | 0x34 = 0x1234
Phase 4: BUS READ  - MemoryRead: r0 ← mem[0x1234] → DONE
Total: 5 ticks
```

### `CAL [0x8000]` (16-bit mode subroutine call)
```
Phase 0: BUS READ  - FetchOpcode → IR, PC++
Phase 1: BUS READ  - FetchOperand16Low: addr_low, PC++
Phase 2: BUS READ  - FetchOperand16High: addr_high, PC++
Phase 3: INTERNAL  - ValueComposition: target_addr = (addr_high << 8) | addr_low
Phase 4: BUS WRITE - MemoryWrite: write PC_high to mem[SP], SP--
Phase 5: BUS WRITE - MemoryWrite: write PC_low to mem[SP], SP-- ; PC ← target_addr → DONE
Total: 6 ticks
```

## Implementation Considerations

### Debugger Integration

The tick-level model enables rich debugging capabilities. Every tick produces a `TickTrace` record captured by `TickHandler` and exposed via `CpuInspector.Traces`. When stepping a full instruction (`CPU.Step()`), all tick traces are concatenated so callers see every micro-step.

**`TickTrace` fields:**

| Field | Description |
|---|---|
| `TickNumber` | Cumulative tick counter across the lifetime of the CPU |
| `Type` | `BusRead`, `BusWrite`, or `Internal` |
| `Phase` | The `MicroPhase` executed on this tick |
| `PcBefore` / `PcAfter` | PC value at the start and end of the tick |
| `SpBefore` / `SpAfter` | SP value at the start and end of the tick |
| `Instruction` | Name of the current opcode (from `OpcodeBaseCode`) |
| `RegisterChanges` | Array of `RegisterChange(Index, OldValue, NewValue)` — only registers that changed |
| `ZeroFlagBefore` / `ZeroFlagAfter` | Zero flag state before and after |
| `CarryFlagBefore` / `CarryFlagAfter` | Carry flag state before and after |
| `InterruptDisableFlagBefore` / `InterruptDisableFlagAfter` | Interrupt disable flag state before and after |
| `Bus` | `BusAccess(Address, Data, Direction)` for bus ticks; `null` for internal ticks |

**Bus recording** is handled transparently via a `BusRecorder` instance wired to `Memory.Recorder` and `Stack.Recorder` by `TickHandler`. Opcodes do not need to report their bus activity — the recorder captures it from the normal `ReadByte`/`WriteByte` calls. Only typed bus calls (the ones opcodes actually use) are recorded; debug helpers like `ReadByte(int)` and `LoadBytes` are not.

**Phase classification** into `TickType` follows these rules:

| `MicroPhase` | `TickType` |
|---|---|
| `FetchOpcode`, `FetchOperand`, `FetchOperand16Low`, `FetchOperand16High`, `MemoryRead` | `BusRead` |
| `MemoryWrite` | `BusWrite` |
| `AluOp`, `EffectiveAddrComputation`, `ValueComposition`, `Done` | `Internal` |

This trace format makes watchpoints, breakpoints, and "why did memory change?" debugging straightforward. See `docs/projects/cpu-simple/debugging.md` for the full pipeline from CPU to IDE.

### Interrupt Handling

**Interrupt rule**: Interrupts may be signaled at any time via `CPU.RequestInterrupt()`, but are serviced only at instruction boundaries when the Interrupt Disable flag (I) is clear.

At each `FetchOpcode` phase:
1. Check `pendingInterrupt` flag AND `!state.I`
2. If both true, execute the ISR sequence (push status+PC to stack, set I, jump to IRQ vector)
3. If interrupt is pending but I is set, the interrupt stays pending until I is cleared
4. Otherwise, fetch the next instruction normally

The ISR pushes a status byte `(I << 2) | (C << 1) | Z` and the current PC to the stack, then sets the I flag (preventing nested interrupts) and jumps to the IRQ vector address. The `RTI` instruction reverses this: it pops PC and status byte, restoring all flags including I.

This avoids the need to save/restore micro-step state. The instruction always completes atomically before interrupt service begins.

### No Dummy Bus Cycles

Unlike real hardware (such as the 6502), this implementation does **not** simulate dummy bus cycles. Real CPUs often perform dummy reads or writes during internal operations to maintain timing consistency with the bus clock.

In CPU-simple:
- Each tick is either a real bus transaction OR an internal operation
- Internal ticks do not generate bus activity
- The tick counts remain accurate to real hardware behavior, but the implementation is simplified

This design choice favors **educational clarity** over hardware quirk simulation. The performance characteristics (instruction timing) match real CPUs, but the bus trace is cleaner and easier to understand.

## Rationale

This tick-level timing model reflects real hardware constraints:

1. **Memory is slower than registers**: Every memory access adds latency (bus tick)
2. **Instruction bytes must be fetched sequentially**: Longer instructions take more bus ticks
3. **Decode is fast**: Simple CPUs use combinational logic that operates in parallel with fetch
4. **Bus ticks are atomic**: Each memory transaction takes time and exclusive bus access
5. **Internal operations have cost**: ALU computations require time; register writes and flag updates do not (they are latching)
6. **Bookkeeping is bundled**: Simple pointer increments (PC++, SP±), register writes, and flag updates can happen during related bus operations without separate ticks

The model maintains CPU-simple's educational simplicity while introducing realistic performance characteristics. This affects program optimization decisions:
- Prefer register operations over memory operations
- Keep frequently-accessed data in registers
- Recognize that 16-bit mode operations cost more due to longer addresses

### Rules of thumb for a single tick's worth of work

- Only a single bus operation can be done per tick (memory read, write).
- Some ticks are internal only; for those no bus operation can be carried
- Decoding is fast and can be bundled with other tick operations (e.g., decoding a register index while fetching the opcode byte)
- Reading and writing to registers is fast and can be bundled with other tick operations
  - This is due to the low number of registers enabling bus access while retaining register file access (using mux and latches)
  - Reads happen at the tick start and writes commit at the end (so writing then reading a register within a tick results in reading the "old" value)
  - This applies to flags and PC as well — they are just special-purpose registers
- ALU work takes time; only a single operation can be carried per tick. The only exception is PC and SP book-keeping (those have dedicated paths and don't "spend" ALU time)
  - Counts as ALU op: add/sub/and/or/xor/shift/compare, effective-address math, PC-relative branch add, etc.
  - Doesn't count (bookkeeping): PC++ after fetch, SP++/-- as part of push/pop

## Summary

**Total instruction cost formula:**
```
Total ticks = Fetch ticks + Execute ticks
            = Instruction size (bytes) + (Bus ticks + Internal ticks)
```

**Key timing factors:**
- Instruction size (1-3 bytes)
- Memory accesses (each is 1 bus tick)
- ALU operations (1 internal tick each)
- Register writes, flag updates, PC writes (free — bundled as latching with any bus tick)
- Addressing mode (direct vs indexed — indexed adds 1 `EffectiveAddrComputation` tick for EA calculation)
- Architecture mode (8-bit vs 16-bit affects instruction sizes and PC width)

This model provides accurate cycle-counting for performance analysis while maintaining clear, debuggable execution semantics.
