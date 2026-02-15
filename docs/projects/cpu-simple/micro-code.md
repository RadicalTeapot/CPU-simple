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

**Bus Tick**
- Performs one memory transaction (read or write)
- May include simple bookkeeping operations that don't violate the single-purpose rule:
  - `PC++` increment after instruction fetch (part of fetch mechanism)
  - `SP++` or `SP--` during stack operations (part of stack transaction)
  - Latching data into registers (`IR`, `op8`, `data`)
  - Simple muxing/selection

**Internal Tick**
- Performs ALU computations and control updates
- Updates architecturally-visible registers
- Computes effective addresses
- Sets or clears flags
- Makes control flow decisions (conditional branches)

### The Boundary: What Requires an Internal Tick?

**✅ Allowed during a bus tick (as "bookkeeping"):**
- `PC++` after each instruction byte fetch
- `SP++` / `SP--` during stack push/pop operations
- Latching data into internal registers (`IR`, `op8`, `op16`, `data`)
- Simple muxing/selection of register sources
- Combinational decode (determining addressing mode, register IDs from opcode bits)

**❌ Requires its own internal tick:**
- Any ALU computation that produces an architecturally meaningful value:
  - Arithmetic operations (ADD, SUB, INC, DEC)
  - Logical operations (AND, OR, XOR)
  - Shifts and rotates
  - Comparisons (they update flags)
  - Effective address calculation for indexed addressing (base + offset)
  - 16-bit value composition in 16-bit mode (may change—see note below)
- Register writebacks that update flags
- PC updates (jumps, calls, returns)

**Pedagogical rule**: *If an operation changes a programmer-visible register/flag or produces an address/value that will be used later, it gets its own internal tick.*

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
- Flag operations: `CLC`, `SEC`, `CLZ`, `SEZ`
- Halt: `HLT`
- No-op: `NOP`

These complete without additional ticks—values are simply latched from one register to another during the decode phase, which is combinational.

**Total cost = Fetch ticks only**

#### Register-only ALU operations: **1 tick (internal)**
Operations that only touch registers (no memory access):
- ALU operations on registers: `ADD Rd, Rs`, `SUB Rd, Rs`, `AND Rd, Rs`, `OR Rd, Rs`, `XOR Rd, Rs`, `CMP Rd, Rs`
- Register shifts/rotates: `LSH Rd`, `RSH Rd`, `LRT Rd`, `RRT Rd`
- Register increment/decrement: `INC Rd`, `DEC Rd`

One internal tick performs:
1. Read source register values (instant)
2. Execute ALU computation
3. Write result to destination register
4. Update flags (Zero, Carry)

**Total cost = Fetch ticks + 1 internal tick**

#### Immediate ALU operations: **1 tick (internal)**
ALU operations with immediate operands:
- Immediate loads: `LDI Rd, #imm`
- Immediate arithmetic: `ADI Rd, #imm`, `SBI Rd, #imm`
- Immediate logic: `ANI Rd, #imm`, `ORI Rd, #imm`, `XRI Rd, #imm`
- Immediate compare: `CPI Rd, #imm`
- Immediate bit test: `BTI Rd, #imm`

The immediate value is fetched during the fetch phase and latched into `op8` (or `op16` in 16-bit mode). One internal tick performs the ALU operation and register writeback.

**Total cost = Fetch ticks + 1 internal tick**

#### Memory read operations: **1 bus tick (read) + 1 internal tick (writeback)**
Operations that read from memory (beyond instruction fetch):
- Memory loads: `LDA Rd, [addr]`, `LDX Rd, [Rs + offset]`
- Memory-based ALU: `ADA Rd, [addr]`, `SBA Rd, [addr]`, `CPA Rd, [addr]`, `ANA Rd, [addr]`, `ORA Rd, [addr]`, `XRA Rd, [addr]`
- Memory bit test: `BTA Rd, [addr]`
- Stack pop: `POP Rd`, `PEK Rd`

Cost breakdown:
1. **Fetch ticks**: Read instruction bytes and address/offset
2. **Internal tick (if indexed addressing)**: Compute effective address (EA = base + offset)
3. **Bus tick**: Read from memory into `data` latch
4. **Internal tick**: Write to destination register, update flags

**Direct addressing total cost = Fetch + 1 bus read + 1 internal**
**Indexed addressing total cost = Fetch + 1 internal (EA calc) + 1 bus read + 1 internal (writeback)**

*Note*: For direct addressing in 16-bit mode, the 16-bit address composition happens during fetch as combinational decode, not as a separate internal tick.

#### Memory write operations: **1 bus tick (write)**
Operations that write to memory:
- Memory stores: `STA Rs, [addr]`, `STX Rs, [Rd + offset]`

Cost breakdown:
1. **Fetch ticks**: Read instruction bytes and address/offset
2. **Internal tick (if indexed addressing)**: Compute effective address (EA = base + offset)
3. **Bus tick**: Write register value to memory

**Direct addressing total cost = Fetch + 1 bus write**
**Indexed addressing total cost = Fetch + 1 internal (EA calc) + 1 bus write**

Register reads happen instantly as part of the bus write operation—no separate internal tick needed.

#### Stack operations

**Push (PSH Rs): 1 bus tick (write)**
1. **Fetch tick**: Read opcode, PC++
2. **Bus tick**: Write Rs to mem[SP], SP-- (bookkeeping)

**Total: 2 ticks**

**Pop (POP Rd): 1 bus tick (read) + 1 internal tick (writeback)**
1. **Fetch tick**: Read opcode, PC++
2. **Bus tick**: Read mem[SP] into `data`, SP++ (bookkeeping)
3. **Internal tick**: Write `data` to Rd, update flags

**Total: 3 ticks**

**Peek (PEK Rd): same as POP but SP unchanged**

**Total: 3 ticks**

#### Control flow operations

**Unconditional jump (JMP [addr]): 1 internal tick (PC update)**

8-bit mode (2 bytes):
1. Fetch opcode, PC++
2. Fetch addr, PC++
3. Internal: PC ← addr

**Total: 3 ticks**

16-bit mode (3 bytes):
1. Fetch opcode, PC++
2. Fetch addr_low, PC++
3. Fetch addr_high, PC++ (addr composition happens here as combinational decode)
4. Internal: PC ← (addr_high << 8) | addr_low

**Total: 4 ticks**

**Conditional jump (JCC, JCS, JZC, JZS [addr])**

Same as JMP, but internal tick only executes if condition is met:

**Taken branch total: same as JMP**
**Not-taken branch total: Fetch ticks only** (no internal PC update)

8-bit mode taken: 3 ticks, not-taken: 2 ticks
16-bit mode taken: 4 ticks, not-taken: 3 ticks

**Subroutine call (CAL [addr])**

Pushes return address to stack, then jumps.

8-bit mode (2 bytes, 8-bit return address):
1. Fetch opcode, PC++
2. Fetch addr, PC++
3. Bus: Write PC to mem[SP], SP-- (bookkeeping)
4. Internal: PC ← addr

**Total: 4 ticks**

16-bit mode (3 bytes, 16-bit return address):
1. Fetch opcode, PC++
2. Fetch addr_low, PC++
3. Fetch addr_high, PC++ (addr composition as combinational decode)
4. Bus: Write PC_low to mem[SP], SP-- (bookkeeping)
5. Bus: Write PC_high to mem[SP], SP-- (bookkeeping)
6. Internal: PC ← (addr_high << 8) | addr_low

**Total: 6 ticks**

**Return from subroutine (RET)**

Pops return address from stack.

8-bit mode (1 byte, 8-bit return address):
1. Fetch opcode, PC++
2. Bus: Read mem[SP] into `data`, SP++ (bookkeeping)
3. Internal: PC ← data

**Total: 3 ticks**

16-bit mode (1 byte, 16-bit return address):
1. Fetch opcode, PC++
2. Bus: Read mem[SP] into PC_low, SP++ (bookkeeping)
3. Bus: Read mem[SP] into PC_high, SP++ (bookkeeping)
4. Internal: PC ← (PC_high << 8) | PC_low

**Total: 4 ticks**

*Note*: The 16-bit composition `(PC_high << 8) | PC_low` currently happens in a single internal tick. This may change in future revisions to separate the composition (1 tick) from the PC update (1 tick), which would add 1 tick to all 16-bit control flow operations.

## Complete Timing Tables

### 8-bit Mode Examples

| Instruction | Bytes | Fetch | Execute | Total | Breakdown |
|-------------|-------|-------|---------|-------|-----------|
| `NOP` | 1 | 1 | 0 | 1 | Fetch only |
| `MOV R0, R1` | 1 | 1 | 0 | 1 | Register latch only |
| `ADD R0, R1` | 1 | 1 | 1 int | 2 | 1 fetch + 1 ALU internal |
| `LDI R0, #0x05` | 2 | 2 | 1 int | 3 | 2 fetch + 1 load internal |
| `LDA R0, [0x0C]` | 2 | 2 | 1 bus + 1 int | 4 | 2 fetch + 1 read + 1 writeback |
| `STA R0, [0x0C]` | 2 | 2 | 1 bus | 3 | 2 fetch + 1 write |
| `ADA R0, [0x10]` | 2 | 2 | 1 bus + 1 int | 4 | 2 fetch + 1 read + 1 ALU+writeback |
| `PSH R0` | 1 | 1 | 1 bus | 2 | 1 fetch + 1 stack write |
| `POP R0` | 1 | 1 | 1 bus + 1 int | 3 | 1 fetch + 1 stack read + 1 writeback |
| `JMP [label]` | 2 | 2 | 1 int | 3 | 2 fetch + 1 PC update |
| `JZS [label]` (taken) | 2 | 2 | 1 int | 3 | 2 fetch + 1 PC update |
| `JZS [label]` (not taken) | 2 | 2 | 0 | 2 | 2 fetch only |
| `CAL [label]` | 2 | 2 | 1 bus + 1 int | 4 | 2 fetch + 1 push + 1 PC update |
| `RET` | 1 | 1 | 1 bus + 1 int | 3 | 1 fetch + 1 pop + 1 PC update |

### 16-bit Mode Examples

Instructions with memory addresses or 16-bit immediates use 3 bytes:

| Instruction | Bytes | Fetch | Execute | Total | Breakdown |
|-------------|-------|-------|---------|-------|-----------|
| `LDA R0, [0x1234]` | 3 | 3 | 1 bus + 1 int | 5 | 3 fetch + 1 read + 1 writeback |
| `STA R0, [0x1234]` | 3 | 3 | 1 bus | 4 | 3 fetch + 1 write |
| `JMP [label]` | 3 | 3 | 1 int | 4 | 3 fetch + 1 PC update |
| `JZS [label]` (taken) | 3 | 3 | 1 int | 4 | 3 fetch + 1 PC update |
| `JZS [label]` (not taken) | 3 | 3 | 0 | 3 | 3 fetch only |
| `CAL [label]` | 3 | 3 | 2 bus + 1 int | 6 | 3 fetch + 2 push (16-bit) + 1 PC update |
| `RET` | 1 | 1 | 2 bus + 1 int | 4 | 1 fetch + 2 pop (16-bit) + 1 PC update |

## Micro-Step Examples

These examples show the tick-by-tick execution using the state machine approach:

### `ADD r0, r1` (register ALU)
```
Phase 0: BUS READ  - fetch opcode → IR, PC++ (PC: 0x00 → 0x01)
Phase 1: INTERNAL  - r0 ← r0 + r1, update flags → DONE
Total: 2 ticks
```

### `LDA r0, [0x0C]` (8-bit mode memory load)
```
Phase 0: BUS READ  - fetch opcode → IR, PC++ (PC: 0x00 → 0x01)
Phase 1: BUS READ  - fetch addr → op8, PC++ (PC: 0x01 → 0x02)
Phase 2: BUS READ  - read mem[0x0C] → data
Phase 3: INTERNAL  - r0 ← data, update flags → DONE
Total: 4 ticks
```

### `JZS rel8` (8-bit mode conditional jump, PC-relative)
```
Phase 0: BUS READ  - fetch opcode → IR, PC++ (PC: 0x10 → 0x11)
Phase 1: BUS READ  - fetch offset → op8, PC++ (PC: 0x11 → 0x12)
Phase 2: INTERNAL  - if Z==0: PC ← PC + signext(op8) → DONE
                     else: DONE (fall through)

Taken:     3 ticks (2 fetch + 1 internal PC update)
Not taken: 2 ticks (2 fetch only)
```

### `CAL [0x0050]` (8-bit mode subroutine call)
```
Phase 0: BUS READ  - fetch opcode → IR, PC++ (PC: 0x20 → 0x21)
Phase 1: BUS READ  - fetch addr → op8, PC++ (PC: 0x21 → 0x22)
Phase 2: BUS WRITE - write PC (0x22) to mem[SP], SP-- (SP: 0xFF → 0xFE)
Phase 3: INTERNAL  - PC ← op8 (0x50) → DONE
Total: 4 ticks
```

### `LDA r0, [0x1234]` (16-bit mode memory load)
```
Phase 0: BUS READ  - fetch opcode → IR, PC++ (PC: 0x0100 → 0x0101)
Phase 1: BUS READ  - fetch addr_low → op8_low, PC++ (PC: 0x0101 → 0x0102)
Phase 2: BUS READ  - fetch addr_high → op8_high, PC++ (PC: 0x0102 → 0x0103)
                     [EA = (op8_high << 8) | op8_low computed as combinational decode]
Phase 3: BUS READ  - read mem[0x1234] → data
Phase 4: INTERNAL  - r0 ← data, update flags → DONE
Total: 5 ticks
```

### `CAL [0x8000]` (16-bit mode subroutine call)
```
Phase 0: BUS READ  - fetch opcode → IR, PC++
Phase 1: BUS READ  - fetch addr_low → op8_low, PC++
Phase 2: BUS READ  - fetch addr_high → op8_high, PC++
                     [target_addr = (op8_high << 8) | op8_low computed as decode]
Phase 3: BUS WRITE - write PC_low to mem[SP], SP--
Phase 4: BUS WRITE - write PC_high to mem[SP], SP--
Phase 5: INTERNAL  - PC ← target_addr → DONE
Total: 6 ticks
```

## Microcode Control Flow

### State Machine Approach

Each instruction is implemented as a state machine where each phase (tick) performs one operation and returns the next phase to execute:

```csharp
public enum MicroPhase {
    DONE,           // Instruction complete
    FETCH_OP8,      // Fetch operand byte
    FETCH_OP16_LOW, // Fetch low byte of 16-bit operand
    FETCH_OP16_HIGH,// Fetch high byte of 16-bit operand
    CALC_EA,        // Calculate effective address (internal)
    MEM_READ,       // Read from memory (bus)
    MEM_WRITE,      // Write to memory (bus)
    ALU_OP,         // Perform ALU operation (internal)
    WRITEBACK,      // Write result to register (internal)
    UPDATE_PC,      // Update program counter (internal)
    // ... more phases as needed
}

// Each opcode implements:
public MicroPhase ExecutePhase(int currentPhase) {
    switch (currentPhase) {
        case 0:
            // First phase after fetch
            return NextPhase();
        case 1:
            // Second phase
            return AnotherPhase();
        // ...
    }
}
```

Conditional flow (like branches) naturally jumps to different phases or directly to `DONE`.

### Shared Micro-Operations

Common operations should be implemented as reusable helper phases to avoid duplication:
- `PUSH_8` / `PUSH_16`: Push 8-bit or 16-bit value to stack
- `POP_8` / `POP_16`: Pop 8-bit or 16-bit value from stack
- `CALC_EA_INDEXED`: Compute base+offset effective address
- `ALU_WRITEBACK`: Perform ALU operation and write result with flags

## Implementation Considerations

### Tick Function

The CPU provides a single `Tick()` function that executes exactly one tick:

```csharp
public void Tick() {
    if (currentPhase == MicroPhase.DONE) {
        // Start new instruction fetch
        BusFetch();  // Fetch first byte into IR, PC++
        currentInstruction = DecodeOpcode(IR);
        currentPhase = currentInstruction.StartPhase();
    } else {
        // Continue current instruction
        currentPhase = currentInstruction.ExecutePhase(currentPhase);
    }

    tickCounter++;
}
```

### Debugger Integration

The tick-level model enables rich debugging capabilities:

**Minimum trace information per tick:**
- Tick number (cumulative counter)
- Tick type: `BUS_READ` / `BUS_WRITE` / `INTERNAL`
- Current phase (micro-step identifier)
- PC before/after
- SP before/after
- Current instruction (IR or decoded instruction name)

**For bus ticks, also log:**
- Bus address
- Bus data value
- Read/Write direction

**For internal ticks, optionally log:**
- Register writeback (which register, new value)
- Flag changes
- ALU operation performed

This trace format makes watchpoints, breakpoints, and "why did memory change?" debugging straightforward.

### Interrupt Handling

**Interrupt rule**: Interrupts may be signaled at any time (external line sampled each tick), but are serviced only at instruction boundaries.

When a tick returns `MicroPhase.DONE`:
1. Check `pendingInterrupt` flag
2. If set, begin interrupt service routine (push PC, jump to vector)
3. Otherwise, fetch next instruction

This avoids the need to save/restore micro-step state. The instruction always completes atomically before interrupt service begins.

### No Dummy Bus Cycles

Unlike real hardware (such as the 6502), this implementation does **not** simulate dummy bus cycles. Real CPUs often perform dummy reads or writes during internal operations to maintain timing consistency with the bus clock.

In CPU-simple:
- Each tick is either a real bus transaction OR an internal operation
- Internal ticks do not generate bus activity
- The tick counts remain accurate to real hardware behavior, but the implementation is simplified

This design choice favors **educational clarity** over hardware quirk simulation. The performance characteristics (instruction timing) match real CPUs, but the bus trace is cleaner and easier to understand.

### Future Enhancement: 16-bit Composition Separation

**Current behavior**: 16-bit value composition `(high << 8) | low` and register/PC update happen in a single internal tick.

**Potential future change**: Separate composition from update into two internal ticks:
1. Internal tick: Compute `temp ← (high << 8) | low`
2. Internal tick: `PC ← temp` (or `Rd ← temp`)

This would add 1 tick to all 16-bit control flow operations (JMP, CAL, RET in 16-bit mode) and some 16-bit data operations. The change would more accurately reflect ALU pipeline stages but adds complexity. This decision is deferred pending implementation experience.

## Rationale

This tick-level timing model reflects real hardware constraints:

1. **Memory is slower than registers**: Every memory access adds latency (bus tick)
2. **Instruction bytes must be fetched sequentially**: Longer instructions take more bus ticks
3. **Decode is "free"**: Simple CPUs use combinational logic that operates in parallel with fetch
4. **Bus ticks are atomic**: Each memory transaction takes time and exclusive bus access
5. **Internal operations have cost**: ALU computations and register updates require time
6. **Bookkeeping is bundled**: Simple pointer increments (PC++, SP±) can happen during related bus operations without separate ticks

The model maintains CPU-simple's educational simplicity while introducing realistic performance characteristics. This affects program optimization decisions:
- Prefer register operations over memory operations
- Keep frequently-accessed data in registers
- Understand that taken branches cost more than not-taken branches
- Recognize that 16-bit mode operations cost more due to longer addresses

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
- Register writebacks with flag updates (1 internal tick)
- Addressing mode (direct vs indexed)
- Branch taken vs not-taken
- Architecture mode (8-bit vs 16-bit affects instruction sizes and PC width)

This model provides accurate cycle-counting for performance analysis while maintaining clear, debuggable execution semantics.
