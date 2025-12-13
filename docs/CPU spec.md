# Spec

8 bit CPU

## Memory mapping

| Address range | Use | Size (in bytes) |
| - | - | - |
| 0x00 - 0xEF | Code + data | 240 | 
| 0xF0 - 0xFF | Stack | 16 |

## Program data

Program loads from 0x00

CPU reset state:
- PC = 0x00
- SP = 0xFF (stack grows downward)

Stack convention:
- PUSH: write value to memory[SP], then decrement SP
- POP: increment SP, then read value from memory[SP]

## Flags

Bits
- 7..2: Not used (for now)
- 1: Carry flag
- 0: Zero flag

Flag update convention (Phase 0):
- Z is set when an operation result is 0x00, cleared otherwise (for ALU-style ops).
- C is set/cleared by operations that naturally produce a carry/borrow (e.g., ADD, shifts).
- For SUB, the exact meaning/behavior of C is **TBD**.
