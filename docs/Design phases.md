## Phase 0 — Write the 1-page spec (so you stop redesigning mid-way)

Decide and record:

* **Memory:** `byte[256]` unified (code+data) Von Neumann
* **State registers:** `PC` (8-bit), `SP` (8-bit), `FLAGS` (at least `Z`, optionally `C`)
* **Register file:** choose **4 GPRs (R0–R3)** to make encoding clean in 1 byte

  * Implication: 4 regs keeps *two-register ops* encodable in 1 byte; 8 regs usually forces longer instructions or fewer opcodes.

Deliverable: `spec.md` with state + flag definitions + memory map (where stack starts, where program loads).

---

## Phase 1 — Pick an instruction encoding that stays readable

With 4 registers, a very teachable encoding is:

### **1-byte base format (reg–reg)**

```
bits:  7..4 = opcode (16 possible)
       3..2 = rd (0..3)
       1..0 = rs (0..3)
```

### **2-byte format (immediate/address)**

Use the first byte as above, and the **second byte** as `imm8` or `addr8` depending on opcode.

Implications:

* Variable-length decode is slightly more work, but your ISA becomes *far* nicer.
* You can still keep execution “simple”: fetch byte1, maybe fetch byte2.

Deliverable: an “opcode map” table (even if some opcodes are reserved initially).

---

## Phase 2 — Define ISA v0 (minimal, but supports CALL/RET properly)

You want enough to write structured programs:

### Data movement

* `MOV rd, rs` (1B)
* `LDI rd, #imm8` (2B)
* `LDR rd, [addr8]` (2B)  (absolute load)
* `STR rs, [addr8]` (2B)  (absolute store)

### ALU + flags

* `ADD rd, rs` (1B)  → sets `Z`, optionally `C`
* `SUB rd, rs` (1B)  → sets `Z`, optionally `C` (borrow convention is a learning choice)
* `CMP rd, rs` (1B)  → sets flags, doesn’t store result (makes branches nicer)

### Control flow

* `JMP addr8` (2B)
* `JZ addr8` / `JNZ addr8` (2B)

### Stack + subroutines (must-have)

* `PUSH rs` (1B) *(use rd field ignored or as extra encoding space)*
* `POP rd` (1B)
* `CALL addr8` (2B)
* `RET` (1B)
* `HLT` (1B)

Stack convention (simple + classic for 8-bit):

* Stack grows **downward** from `0xFF`
* `PUSH`: `mem[SP] = value; SP--`
* `POP`: `SP++; value = mem[SP]`
* `CALL addr`: push return address (the PC *after* the operand byte), then `PC = addr`
* `RET`: pop return address into PC

Deliverable: an ISA table: instruction, bytes, pseudo-code semantics, flags affected.

---

## Phase 3 — Implement the emulator core in C# (with “education hooks”)

### CPU state model

* `byte[] R = new byte[4];`
* `byte PC, SP;`
* `bool Z, C;`
* `byte[] mem = new byte[256];`

### Execution model

Start with a clean “step” API:

* `Step()` executes exactly one instruction and returns (or throws on HLT)
* Optionally include a **trace** object each step: `{PC_before, opcode, operands, regs_before/after, mem_writes}`

Implications:

* Traces make debugging + learning *dramatically* easier than staring at memory dumps.

Deliverable: `Cpu.Step()` + a tiny runner that executes until HLT or max-steps.

---

## Phase 4 — Build tests before you build an assembler

Do two layers:

1. **Instruction unit tests**
   For each opcode: set initial CPU state → step → assert registers/flags/memory/PC.

2. **Small “ROM tests”** (programs)
   Load a byte array into memory, run, assert final state.

Deliverable: a `tests/` suite that you can keep forever as you extend the ISA.

---

## Phase 5 — Write a tiny assembler (2-pass, labels) just enough to be productive

Since your ISA is simple, an assembler is a perfect learning milestone:

* Pass 1: parse lines, compute addresses, collect labels
* Pass 2: emit bytes, resolve label operands

Start with:

* Labels (`loop:`)
* `.org`, `.byte`
* A handful of mnemonics

Deliverable: `asm → byte[256]` + a way to dump to hex for inspection.

---

## Phase 6 — Write “real” example programs (forces ISA sanity)

Minimum set:

* Sum an array in memory (tests loops + loads/stores)
* A function call with parameters in registers (tests CALL/RET + convention)
* Nested calls (tests stack correctness)
* Optional: simple “stdlib” routines (e.g., `memcpy`, `add16` later)

Deliverable: `examples/` with source + expected output state.

---

## Phase 7 — Iterate deliberately (one new concept at a time)

Good next extensions (each teaches something distinct):

* **Indexed addressing**: `LDR rd, [addr + R0]` (arrays become natural)
* **More flags + branches**: carry/negative/overflow decisions
* **Memory-mapped I/O**: e.g., `0xF0` = output port (lets you “print”)
* **8 → 16-bit addresses**: new exercise; you’ll feel exactly what changes (PC width, CALL/RET pushes 2 bytes, jump operands, etc.)

---

## Extra choices

* Use 4 registers
* Use a dedicated SP
* Include a CMP instruction