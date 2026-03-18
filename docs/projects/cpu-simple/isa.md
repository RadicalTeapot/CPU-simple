# ISA

## Ideas

### Original

This was the original ISA, it used the high nibble as a way to differentiate between opcodes.

It is very simple and probably too limited to be used in an efficient way given a limited amount of program memory.

| Mnemonic | Size | High nibble | Register number (bit 3 and 2) | Register number (bit 1 and 0) | Second byte | Notes |
| - | - | - | - | - | - | - |
| NOP | B | 0 |   -  |   -  |   -   | |
| MOV | B | 1 | DEST | SRC  |   -   | |
| LDI | W | 2 | DEST |   -  | value | |
| LDR | W | 3 | DEST |   -  |  addr | |
| STR | W | 4 | SRC  |   -  |  addr | |
| ADD | B | 5 | DEST | SRC  |   -   | Sets Z and C |
| SUB | B | 6 | DEST | SRC  |   -   | Sets Z (C is TBD for SUB) |
| CMP | B | 7 | DEST | SRC  |   -   | Only sets Z and C, doesn't store result |
| JMP | W | 8 |   -  |   -  |  addr | |
| JEZ | W | 9 |   -  |   -  |  addr | |
| JNZ | W | A |   -  |   -  |  addr | |
| PSH | B | B | SRC  |   -  |   -   | mem[SP]=value; SP-- |
| POP | B | C | DEST |   -  |   -   | SP++; value=mem[SP] |
| CAL | W | D |   -  |   -  |  addr | push PC+2 then PC = addr |
| RET | B | E |   -  |   -  |   -   | pop into PC |
| HLT | B | F |   -  |   -  |   -   | |

### Rev 1

The idea here was to use some of the free bits of each opcodes to increase the amount of opcodes that can be represented with the same instruction size (1 to 2 bytes).
This lead to the idea of grouping commands by their type and adding some structure to the way opcodes are built on a bit level (i.e. all flag `clear` commands have their bit 0 set to 0, or bit2..1 being `01` for carry and `10` for zero flags).

The tradeoff is the parsing logic is more involved.

All additions are done with carry (`source+destination+carry`) to allow for easy multi-byte addition. **Do not forget to clear carry before non-multi byte addition**.

Subtraction uses a no borrow carry (i.e. carry is set to 1 if no borrow occurred) to allow for easy multi-byte subtraction. **Do not forget to set carry before non-multi byte subtraction**.

Compare carry behavior is `SUB` without setting value (i.e. set carry if destination is greater or equal to source).

The excel sheet ISA is [here](assets/other/ISA.xlsx)

![[assets/images/cpu-simple-isa-rev-1.png]]

---

## Current ISA Reference

### Architecture Overview

**Registers:** 4 general-purpose registers `R0`–`R3`, each 8 bits wide.

**Flags:**
| Flag | Bit | Description |
|------|-----|-------------|
| `Z` | 0 | Zero — set when an operation result is zero (or bits match for BTI/BTA) |
| `C` | 1 | Carry — set on arithmetic carry-out or shift-out; no-borrow for subtraction/compare |
| `I` | 2 | Interrupt Disable — when set, maskable interrupts are deferred |

**Memory map (8-bit mode):**
- `0x00`–`0xEF`: RAM / program code
- `0xF0`–`0xFF`: MMIO registers (PPU, peripherals); stack grows down from `0xFF`

**Memory map (16-bit mode):**
- `0x0000`–`0xFEFF`: RAM / program code
- `0xFF00`–`0xFFFF`: MMIO registers; stack grows down from `0xFFFF`

**Instruction size:** 1 byte (no operands), 2 bytes (immediate or 8-bit address), 3 bytes (16-bit address in 16-bit mode). Tick counts below are for **8-bit mode**; 16-bit mode adds 2 ticks to all address-carrying instructions due to the extra address byte and a ValueComposition internal tick.

---

### Cheatsheet

Syntax conventions: `Rd` = destination register, `Rs` = source register, `#imm` = immediate byte, `[addr]` = direct memory address, `[Rs + #off]` = indexed address (base register plus 6-bit offset).

#### System and Control Flow

| Mnemonic | Syntax | Opcode | Ticks | Flags | Summary |
|----------|--------|--------|-------|-------|---------|
| `NOP` | `nop` | `0x00` | 1 | — | No operation |
| `HLT` | `hlt` | `0x01` | 1 | — | Halt CPU |
| `CLC` | `clc` | `0x02` | 1 | C=0 | Clear carry |
| `SEC` | `sec` | `0x03` | 1 | C=1 | Set carry |
| `CLZ` | `clz` | `0x04` | 1 | Z=0 | Clear zero |
| `SEZ` | `sez` | `0x05` | 1 | Z=1 | Set zero |
| `SEI` | `sei` | `0x06` | 1 | I=1 | Disable interrupts |
| `CLI` | `cli` | `0x07` | 1 | I=0 | Enable interrupts |
| `JMP` | `jmp [addr]` | `0x08` | 2 | — | Unconditional jump |
| `RTI` | `rti` | `0x09` | 3 | Z,C,I | Return from interrupt |
| `JCC` | `jcc [addr]` | `0x0A` | 2 | — | Jump if C clear (C=0) |
| `JCS` | `jcs [addr]` | `0x0B` | 2 | — | Jump if C set (C=1) |
| `JZC` | `jzc [addr]` | `0x0C` | 2 | — | Jump if Z clear (Z=0) |
| `JZS` | `jzs [addr]` | `0x0D` | 2 | — | Jump if Z set (Z=1) |
| `CAL` | `cal [addr]` | `0x0E` | 3 | — | Call subroutine |
| `RET` | `ret` | `0x0F` | 2 | — | Return from subroutine |

#### Load / Store / Move

| Mnemonic | Syntax | Opcode | Ticks | Flags | Summary |
|----------|--------|--------|-------|-------|---------|
| `LDI` | `ldi Rd, #imm` | `0x10+Rd` | 2 | — | Load immediate into register |
| `LDA` | `lda Rd, [addr]` | `0x14+Rd` | 3 | — | Load from memory address |
| `POP` | `pop Rd` | `0x18+Rd` | 2 | — | Pop from stack into register |
| `PEK` | `pek Rd` | `0x1C+Rd` | 2 | — | Peek top of stack (no SP change) |
| `PSH` | `psh Rs` | `0x20+Rs` | 2 | — | Push register onto stack |
| `STA` | `sta Rs, [addr]` | `0x24+Rs` | 3 | — | Store register to memory address |
| `LDX` | `ldx Rd, [Rs + #off]` | `0x28+Rd` | 4 | — | Indexed load |
| `STX` | `stx Rs, [Rb + #off]` | `0x2C+Rs` | 4 | — | Indexed store |
| `MOV` | `mov Rd, Rs` | `0x30+(Rs<<2)+Rd` | 1 | — | Copy register to register |

#### Arithmetic

| Mnemonic | Syntax | Opcode | Ticks | Flags | Summary |
|----------|--------|--------|-------|-------|---------|
| `ADI` | `adi Rd, #imm` | `0x40+Rd` | 3 | Z,C | Add immediate with carry |
| `ADA` | `ada Rd, [addr]` | `0x44+Rd` | 4 | Z,C | Add memory with carry |
| `SBI` | `sbi Rd, #imm` | `0x48+Rd` | 3 | Z,C | Subtract immediate with borrow |
| `SBA` | `sba Rd, [addr]` | `0x4C+Rd` | 4 | Z,C | Subtract memory with borrow |
| `ADD` | `add Rd, Rs` | `0x50+(Rs<<2)+Rd` | 2 | Z,C | Add registers with carry |
| `SUB` | `sub Rd, Rs` | `0x60+(Rs<<2)+Rd` | 2 | Z,C | Subtract registers with borrow |
| `INC` | `inc Rd` | `0xE0+Rd` | 2 | Z | Increment register |
| `DEC` | `dec Rd` | `0xE4+Rd` | 2 | Z | Decrement register |

#### Shifts and Rotates

| Mnemonic | Syntax | Opcode | Ticks | Flags | Summary |
|----------|--------|--------|-------|-------|---------|
| `LSH` | `lsh Rd` | `0x70+Rd` | 2 | C | Left shift (MSB → C, 0 into LSB) |
| `RSH` | `rsh Rd` | `0x74+Rd` | 2 | C | Right shift (LSB → C, 0 into MSB) |
| `LRT` | `lrt Rd` | `0x78+Rd` | 2 | — | Left rotate (MSB wraps to LSB) |
| `RRT` | `rrt Rd` | `0x7C+Rd` | 2 | — | Right rotate (LSB wraps to MSB) |

#### Compare

| Mnemonic | Syntax | Opcode | Ticks | Flags | Summary |
|----------|--------|--------|-------|-------|---------|
| `CPI` | `cpi Rd, #imm` | `0x80+Rd` | 3 | Z,C | Compare register with immediate |
| `CPA` | `cpa Rd, [addr]` | `0x84+Rd` | 4 | Z,C | Compare register with memory |
| `CMP` | `cmp Rd, Rs` | `0x90+(Rs<<2)+Rd` | 2 | Z,C | Compare two registers |

#### Logical

| Mnemonic | Syntax | Opcode | Ticks | Flags | Summary |
|----------|--------|--------|-------|-------|---------|
| `ANI` | `ani Rd, #imm` | `0x88+Rd` | 3 | Z | AND register with immediate |
| `ANA` | `ana Rd, [addr]` | `0x8C+Rd` | 4 | Z | AND register with memory |
| `AND` | `and Rd, Rs` | `0xA0+(Rs<<2)+Rd` | 2 | Z | AND two registers |
| `ORI` | `ori Rd, #imm` | `0xB0+Rd` | 3 | Z | OR register with immediate |
| `ORA` | `ora Rd, [addr]` | `0xB4+Rd` | 4 | Z | OR register with memory |
| `OR` | `or Rd, Rs` | `0xC0+(Rs<<2)+Rd` | 2 | Z | OR two registers |
| `XRI` | `xri Rd, #imm` | `0xB8+Rd` | 3 | Z | XOR register with immediate |
| `XRA` | `xra Rd, [addr]` | `0xBC+Rd` | 4 | Z | XOR register with memory |
| `XOR` | `xor Rd, Rs` | `0xD0+(Rs<<2)+Rd` | 2 | Z | XOR two registers |
| `BTI` | `bti Rd, #imm` | `0xE8+Rd` | 3 | Z | Bit test register against immediate |
| `BTA` | `bta Rd, [addr]` | `0xEC+Rd` | 4 | Z | Bit test register against memory |

---

### Detailed Reference

#### Opcode Encoding

**No register** (SystemAndJump group, mask `0xFF`): full opcode byte is the instruction; no register bits.

**One register** (mask `0xFC`): bits `[1:0]` of the opcode byte encode the register index (`0`=R0, `1`=R1, `2`=R2, `3`=R3). The base code occupies bits `[7:2]`.

**Two registers** (mask `0xF0`): bits `[3:2]` encode the source register, bits `[1:0]` encode the destination register. The base code occupies bits `[7:4]`.

**Indexed addressing operand byte** (LDX / STX): bits `[1:0]` = base register index, bits `[7:2]` = 6-bit immediate offset (range 0–63).

---

#### NOP — No Operation
```
nop
```
Does nothing. Useful for timing padding.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 1 | — |

---

#### HLT — Halt
```
hlt
```
Stops CPU execution immediately. The CPU raises a halt exception; the emulator transitions to an error/halted state.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 1 | — |

---

#### CLC / SEC — Clear / Set Carry
```
clc    ; C ← 0
sec    ; C ← 1
```
Directly manipulates the carry flag. Always use `sec` before a multi-byte subtraction chain and `clc` before a multi-byte addition chain.

| | Bytes | Ticks | Flags |
|-|-------|-------|-------|
| CLC | 1 | 1 | C=0 |
| SEC | 1 | 1 | C=1 |

---

#### CLZ / SEZ — Clear / Set Zero
```
clz    ; Z ← 0
sez    ; Z ← 1
```
Directly manipulates the zero flag.

| | Bytes | Ticks | Flags |
|-|-------|-------|-------|
| CLZ | 1 | 1 | Z=0 |
| SEZ | 1 | 1 | Z=1 |

---

#### CLI / SEI — Enable / Disable Interrupts
```
cli    ; I ← 0  (interrupts enabled)
sei    ; I ← 1  (interrupts disabled)
```
Controls the interrupt disable flag. When `I=1`, maskable interrupt requests are held pending until `cli` is executed.

| | Bytes | Ticks | Flags |
|-|-------|-------|-------|
| CLI | 1 | 1 | I=0 |
| SEI | 1 | 1 | I=1 |

---

#### JMP — Unconditional Jump
```
jmp [addr]    ; PC ← addr
```
Sets the program counter to the target address unconditionally.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 2 / 4 | — |

---

#### JCC / JCS / JZC / JZS — Conditional Jumps
```
jcc [addr]    ; if C=0: PC ← addr
jcs [addr]    ; if C=1: PC ← addr
jzc [addr]    ; if Z=0: PC ← addr
jzs [addr]    ; if Z=1: PC ← addr
```
Conditional branch to the target address. If the condition is not met, execution continues at the next instruction. Flag evaluation is combinational (no extra tick for the check).

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 2 / 4 | — |

---

#### CAL — Call Subroutine
```
cal [addr]    ; push PC; PC ← addr
```
Pushes the return address (address of the next instruction after `cal`) onto the stack, then jumps to the target address.

In 16-bit mode the return address is pushed as two bytes: high byte first, low byte second (so `ret` pops low first).

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 3 / 6 | — |

---

#### RET — Return from Subroutine
```
ret    ; PC ← pop()
```
Pops the return address from the stack and loads it into the program counter.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 1 | 2 / 4 | — |

---

#### RTI — Return from Interrupt
```
rti    ; PC ← pop(); flags ← pop()
```
Pops the return address and then the status byte from the stack, restoring the program counter and all flags. The status byte layout pushed by the interrupt handler is:

| Bit 2 | Bit 1 | Bit 0 |
|-------|-------|-------|
| I | C | Z |

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 1 | 3 / 5 | Z, C, I |

---

#### LDI — Load Immediate
```
ldi Rd, #imm    ; Rd ← imm
```
Loads an 8-bit immediate value into the destination register.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 2 | — |

---

#### LDA — Load from Address
```
lda Rd, [addr]    ; Rd ← mem[addr]
```
Loads the byte at the given memory address into the destination register. Address is a label or hex literal.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 3 / 5 | — |

---

#### STA — Store to Address
```
sta Rs, [addr]    ; mem[addr] ← Rs
```
Stores the source register value at the given memory address.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 3 / 5 | — |

---

#### LDX — Indexed Load
```
ldx Rd, [Rs + #off]    ; Rd ← mem[Rs + off]
```
Loads the byte at the effective address `Rs + off` into `Rd`. The base register `Rs` and the 6-bit immediate offset `off` (0–63) are encoded together in a single operand byte: bits `[1:0]` = `Rs` index, bits `[7:2]` = offset.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 4 | — |

**Operand byte encoding:** `(off << 2) | Rs_idx`

---

#### STX — Indexed Store
```
stx Rs, [Rb + #off]    ; mem[Rb + off] ← Rs
```
Stores `Rs` at the effective address `Rb + off`. The base register `Rb` and offset are packed into the operand byte identically to LDX. `Rs` (the value register) is encoded in the opcode byte.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 4 | — |

**Operand byte encoding:** `(off << 2) | Rb_idx`

---

#### PSH — Push to Stack
```
psh Rs    ; mem[SP] ← Rs; SP--
```
Pushes the register value onto the stack and decrements the stack pointer.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | — |

---

#### POP — Pop from Stack
```
pop Rd    ; SP++; Rd ← mem[SP]
```
Increments the stack pointer and loads the value at the top of the stack into the destination register.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | — |

---

#### PEK — Peek Stack
```
pek Rd    ; Rd ← mem[SP]  (SP unchanged)
```
Reads the top of the stack into `Rd` without modifying the stack pointer.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | — |

---

#### MOV — Move Register
```
mov Rd, Rs    ; Rd ← Rs
```
Copies the value of `Rs` into `Rd`. Does not affect flags.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 1 | — |

---

#### ADD — Add Registers
```
add Rd, Rs    ; Rd ← Rd + Rs + C
```
Adds `Rs` and the carry flag to `Rd`. Result wraps around at 256. Sets `C` if the true result exceeds 255, sets `Z` if the result is zero.

> **Multi-byte addition:** chain multiple `add` instructions — carry propagates naturally from the lower byte. Clear carry with `clc` before the first byte of a new addition.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z, C |

---

#### ADI — Add Immediate
```
adi Rd, #imm    ; Rd ← Rd + imm + C
```
Same as `add` but the second operand is an 8-bit immediate value.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 3 | Z, C |

---

#### ADA — Add Memory
```
ada Rd, [addr]    ; Rd ← Rd + mem[addr] + C
```
Same as `add` but the second operand is read from a memory address.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 4 / 6 | Z, C |

---

#### SUB — Subtract Registers
```
sub Rd, Rs    ; Rd ← Rd - Rs - (1 - C)
```
Subtracts `Rs` and the inverted carry (borrow) from `Rd`. Result wraps at 0. Sets `C` if no borrow occurred (`Rd >= Rs + borrow`), sets `Z` if result is zero.

> **No-borrow carry:** C=1 means no borrow (result ≥ 0). Set carry with `sec` before the first byte of a standalone subtraction; borrow then propagates automatically in multi-byte chains.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z, C |

---

#### SBI — Subtract Immediate
```
sbi Rd, #imm    ; Rd ← Rd - imm - (1 - C)
```
Same as `sub` but the second operand is an 8-bit immediate.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 3 | Z, C |

---

#### SBA — Subtract Memory
```
sba Rd, [addr]    ; Rd ← Rd - mem[addr] - (1 - C)
```
Same as `sub` but the second operand is read from a memory address.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 4 / 6 | Z, C |

---

#### INC — Increment
```
inc Rd    ; Rd ← Rd + 1
```
Increments the register by one. Wraps from `0xFF` to `0x00`. Sets `Z` if the result is zero. Does **not** affect carry.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z |

---

#### DEC — Decrement
```
dec Rd    ; Rd ← Rd - 1
```
Decrements the register by one. Wraps from `0x00` to `0xFF`. Sets `Z` if the result is zero. Does **not** affect carry.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z |

---

#### LSH — Left Shift
```
lsh Rd    ; C ← Rd[7]; Rd ← Rd << 1
```
Shifts `Rd` left by one bit; the most-significant bit is shifted into the carry flag, and a zero is shifted into the least-significant bit. Does **not** affect zero flag.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | C |

---

#### RSH — Right Shift
```
rsh Rd    ; C ← Rd[0]; Rd ← Rd >> 1
```
Shifts `Rd` right by one bit; the least-significant bit is shifted into the carry flag, and a zero is shifted into the most-significant bit. Does **not** affect zero flag.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | C |

---

#### LRT — Left Rotate
```
lrt Rd    ; Rd ← (Rd << 1) | Rd[7]
```
Rotates `Rd` left by one bit; the most-significant bit wraps around into the least-significant position. Does **not** affect any flag.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | — |

---

#### RRT — Right Rotate
```
rrt Rd    ; Rd ← (Rd >> 1) | (Rd[0] << 7)
```
Rotates `Rd` right by one bit; the least-significant bit wraps around into the most-significant position. Does **not** affect any flag.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | — |

---

#### CMP — Compare Registers
```
cmp Rd, Rs    ; Z ← (Rd == Rs); C ← (Rd >= Rs)
```
Performs a subtraction `Rd - Rs` and sets flags without storing the result. Carry semantics follow `sub`: C=1 means no borrow (Rd ≥ Rs).

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z, C |

---

#### CPI — Compare Immediate
```
cpi Rd, #imm    ; Z ← (Rd == imm); C ← (Rd >= imm)
```
Same as `cmp` but compares with an 8-bit immediate.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 3 | Z, C |

---

#### CPA — Compare Memory
```
cpa Rd, [addr]    ; Z ← (Rd == mem[addr]); C ← (Rd >= mem[addr])
```
Same as `cmp` but compares with a value read from memory.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 4 / 6 | Z, C |

---

#### AND — AND Registers
```
and Rd, Rs    ; Rd ← Rd & Rs
```
Bitwise AND. Sets `Z` if the result is zero.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z |

---

#### ANI — AND Immediate
```
ani Rd, #imm    ; Rd ← Rd & imm
```
Bitwise AND with an immediate value. Sets `Z` if the result is zero.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 3 | Z |

---

#### ANA — AND Memory
```
ana Rd, [addr]    ; Rd ← Rd & mem[addr]
```
Bitwise AND with a value from memory. Sets `Z` if the result is zero.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 4 / 6 | Z |

---

#### OR — OR Registers
```
or Rd, Rs    ; Rd ← Rd | Rs
```
Bitwise OR. Sets `Z` if the result is zero.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z |

---

#### ORI — OR Immediate
```
ori Rd, #imm    ; Rd ← Rd | imm
```
Bitwise OR with an immediate value. Sets `Z` if the result is zero.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 3 | Z |

---

#### ORA — OR Memory
```
ora Rd, [addr]    ; Rd ← Rd | mem[addr]
```
Bitwise OR with a value from memory. Sets `Z` if the result is zero.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 4 / 6 | Z |

---

#### XOR — XOR Registers
```
xor Rd, Rs    ; Rd ← Rd ^ Rs
```
Bitwise XOR. Sets `Z` if the result is zero.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 1 | 2 | Z |

---

#### XRI — XOR Immediate
```
xri Rd, #imm    ; Rd ← Rd ^ imm
```
Bitwise XOR with an immediate value. Sets `Z` if the result is zero.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 3 | Z |

---

#### XRA — XOR Memory
```
xra Rd, [addr]    ; Rd ← Rd ^ mem[addr]
```
Bitwise XOR with a value from memory. Sets `Z` if the result is zero.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 4 / 6 | Z |

---

#### BTI — Bit Test Immediate
```
bti Rd, #imm    ; Z ← ((Rd & imm) != 0)
```
Tests bits of `Rd` against a bitmask immediate. Sets `Z=1` if **any masked bits are set** (AND result is non-zero); sets `Z=0` if no masked bits are set. Does **not** modify `Rd`.

> **Note:** the zero flag is set to indicate a positive match, which is the opposite of the carry-based "set if zero" convention on some other architectures. Use `jzs` to branch when bits are present, `jzc` to branch when they are absent.

| Bytes | Ticks | Flags |
|-------|-------|-------|
| 2 | 3 | Z |

---

#### BTA — Bit Test Memory
```
bta Rd, [addr]    ; Z ← ((Rd & mem[addr]) != 0)
```
Same as `bti` but the bitmask is read from a memory address.

| Bytes (8-bit / 16-bit) | Ticks (8-bit / 16-bit) | Flags |
|------------------------|------------------------|-------|
| 2 / 3 | 4 / 6 | Z |
