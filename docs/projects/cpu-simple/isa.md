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