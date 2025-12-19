# Registers

## Program counter (PC)

As this is a "pure" 8bit CPU, the program counter (`PC`) register is 1 byte wide.

## General purpose registers (GPR)

The current design has 4 general purpose registers (`GPR`), each 1 byte wide.

## Flags

There is a single ,1 byte wide, flags register.

Opcodes that don't follow the conventions will have their use of the flags documented.

### Bits

| 7   | 6   | 5   | 4   | 3   | 2   | 1     | 0    |
| --- | --- | --- | --- | --- | --- | ----- | ---- |
| -   | -   | -   | -   | -   | -   | Carry | Zero |

### Convention

- Z is set when an operation result is 0x00, cleared otherwise (for ALU-style ops).
- C is set/cleared by operations that naturally produce a carry/borrow (e.g., ADD, shifts) and by the clear and set opcodes.
- Subtraction uses a no borrow carry (i.e. carry is set to 1 if no borrow occurred) to allow for easy multi-byte subtraction