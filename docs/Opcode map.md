# Opcode map

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