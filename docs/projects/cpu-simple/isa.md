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

| Group                      | Mnemonic | 7   | 6   | 5   | 4   | 3   | 2   | 1   | 0   | Byte 2 | Size | Opcode | Affected flags       | Function                                   |
| -------------------------- | -------- | --- | --- | --- | --- | --- | --- | --- | --- | ------ | ---- | ------ | -------------------- | ------------------------------------------ |
| SYSTEM                     | NOP      | 0   | 0   | 0   | 0   | 0   | 0   | 0   | 0   | NA     | 1    | 00     |                      | no operation                               |
| SYSTEM                     | HLT      | 0   | 0   | 0   | 0   | 0   | 0   | 0   | 1   | NA     | 1    | 01     |                      | stops CPU                                  |
| SYSTEM                     | CLC      | 0   | 0   | 0   | 0   | 0   | 0   | 1   | 0   | NA     | 1    | 02     | C                    | clear carry flag                           |
| SYSTEM                     | SEC      | 0   | 0   | 0   | 0   | 0   | 0   | 1   | 1   | NA     | 1    | 03     | C                    | set carry flag                             |
| SYSTEM                     | CLZ      | 0   | 0   | 0   | 0   | 0   | 1   | 0   | 0   | NA     | 1    | 04     | Z                    | clear zero flag                            |
| SYSTEM                     | SEZ      | 0   | 0   | 0   | 0   | 0   | 1   | 0   | 1   | NA     | 1    | 05     | Z                    | set zero flag                              |
| SYSTEM                     |          | 0   | 0   | 0   | 0   | 0   | 1   | 1   | 0   | NA     | 1    | 06     |                      | reserved                                   |
| SYSTEM                     |          | 0   | 0   | 0   | 0   | 0   | 1   | 1   | 1   | NA     | 1    | 07     |                      | reserved                                   |
| JUMP                       | JMP      | 0   | 0   | 0   | 0   | 1   | 0   | 0   | 0   | addr   | 2    | 08     |                      | Jump to address                            |
| JUMP                       |          | 0   | 0   | 0   | 0   | 1   | 0   | 0   | 1   | addr   | 2    | 09     |                      | reserved                                   |
| JUMP                       | JCC      | 0   | 0   | 0   | 0   | 1   | 0   | 1   | 0   | addr   | 2    | 0A     |                      | Jump if carry clear                        |
| JUMP                       | JCS      | 0   | 0   | 0   | 0   | 1   | 0   | 1   | 1   | addr   | 2    | 0B     |                      | Jump if carry set                          |
| JUMP                       | JZC      | 0   | 0   | 0   | 0   | 1   | 1   | 0   | 0   | addr   | 2    | 0C     |                      | Jump if zero clear                         |
| JUMP                       | JZS      | 0   | 0   | 0   | 0   | 1   | 1   | 0   | 1   | addr   | 2    | 0D     |                      | Jump if zero set                           |
| JUMP                       | CAL      | 0   | 0   | 0   | 0   | 1   | 1   | 1   | 0   | addr   | 2    | 0E     |                      | Push PC onto stack and jump to address     |
| JUMP                       | RET      | 0   | 0   | 0   | 0   | 1   | 1   | 1   | 1   | NA     | 1    | 0F     |                      | Pop PC from stack                          |
| LOAD                       | LDI      | 0   | 0   | 0   | 1   | 0   | 0   | rd  | rd  | imm    | 2    | 10..13 |                      | Load immediate to register                 |
| LOAD                       | LDR      | 0   | 0   | 0   | 1   | 0   | 1   | rd  | rd  | addr   | 2    | 14..17 |                      | Load address content to register           |
| LOAD                       | POP      | 0   | 0   | 0   | 1   | 1   | 0   | rd  | rd  | NA     | 1    | 18..1B |                      | Pop stack to register                      |
| LOAD                       |          | 0   | 0   | 0   | 1   | 1   | 1   | \-  | \-  | NA     | 1    | 1C..1F |                      | reserved                                   |
| STORE                      |          | 0   | 0   | 1   | 0   | 0   | 0   | \-  | \-  | NA     | 1    | 20..23 |                      | reserved                                   |
| STORE                      | STR      | 0   | 0   | 1   | 0   | 0   | 1   | rs  | rs  | addr   | 2    | 24..27 |                      | Store register to address                  |
| STORE                      | PUSH     | 0   | 0   | 1   | 0   | 1   | 0   | rs  | rs  | NA     | 1    | 28..2B |                      | Push register to stack                     |
| STORE                      |          | 0   | 0   | 1   | 0   | 1   | 1   | \-  | \-  | NA     | 1    | 2C..2F |                      | reserved                                   |
| MOVE                       | MOV      | 0   | 0   | 1   | 1   | rs  | rs  | rd  | rd  | NA     | 1    | 30..3F |                      | Move source to destination                 |
| SINGLE REGISTER ARITHMETIC | ADI      | 0   | 1   | 0   | 0   | 0   | 0   | rd  | rd  | imm    | 2    | 40..43 | C,Z                  | Add immediate to register                  |
| SINGLE REGISTER ARITHMETIC | ADA      | 0   | 1   | 0   | 0   | 0   | 1   | rd  | rd  | addr   | 2    | 44..47 | C,Z                  | Add address to register                    |
| SINGLE REGISTER ARITHMETIC | SBI      | 0   | 1   | 0   | 0   | 1   | 0   | rd  | rd  | imm    | 2    | 48..4B | C (no borrow),Z      | Subtract immediate from register           |
| SINGLE REGISTER ARITHMETIC | SBA      | 0   | 1   | 0   | 0   | 1   | 1   | rd  | rd  | addr   | 2    | 4C..4F | C (no borrow),Z      | Subtract address from register             |
| MULTI REGISTER ARITHMETIC  | ADD      | 0   | 1   | 0   | 1   | rs  | rs  | rd  | rd  | NA     | 1    | 50..5F | C,Z                  | Add source to destination                  |
| MULTI REGISTER ARITHMETIC  | SUB      | 0   | 1   | 1   | 0   | rs  | rs  | rd  | rd  | NA     | 1    | 60..6F | C (no borrow),Z      | Subtract source from destination           |
| BITS MANIPULATION          | LSH      | 0   | 1   | 1   | 1   | 0   | 0   | rd  | rd  | NA     | 1    | 70..73 | C, Z                 | Left shift, carry set to bit shifted out   |
| BITS MANIPULATION          | RSH      | 0   | 1   | 1   | 1   | 0   | 1   | rd  | rd  | NA     | 1    | 74..77 | C, Z                 | Right shift, carry set to bit shifted out  |
| BITS MANIPULATION          | LRT      | 0   | 1   | 1   | 1   | 1   | 0   | rd  | rd  | NA     | 1    | 78..7B | Z                    | Rotate left, carry unaffected              |
| BITS MANIPULATION          | RRT      | 0   | 1   | 1   | 1   | 1   | 1   | rd  | rd  | NA     | 1    | 7C..7F | Z                    | Rotate right, carry unaffected             |
| SINGLE REGISTER LOGIC I    | CPI      | 1   | 0   | 0   | 0   | 0   | 0   | rd  | rd  | imm    | 2    | 80..83 | C (d \>= s),Z(s = d) | Compare immediate, set flags but not value |
| SINGLE REGISTER LOGIC I    | CPA      | 1   | 0   | 0   | 0   | 0   | 1   | rd  | rd  | addr   | 2    | 84..87 | C (d \>= s),Z(s = d) | Compare address, set flags but not value   |
| SINGLE REGISTER LOGIC I    | ANI      | 1   | 0   | 0   | 0   | 1   | 0   | rd  | rd  | imm    | 2    | 88..8B | Z                    | And immediate                              |
| SINGLE REGISTER LOGIC I    | ANA      | 1   | 0   | 0   | 0   | 1   | 1   | rd  | rd  | addr   | 2    | 8C..8F | Z                    | And address                                |
| COMPARE                    | CMP      | 1   | 0   | 0   | 1   | rs  | rs  | rd  | rd  | NA     | 1    | 90..9F | C (d \>= s),Z(s = d) | Compare, set flags but not value           |
| AND                        | AND      | 1   | 0   | 1   | 0   | rs  | rs  | rd  | rd  | NA     | 1    | A0..AF | Z                    | And                                        |
| SINGLE REGISTER LOGIC II   | ORI      | 1   | 0   | 1   | 1   | 0   | 0   | rd  | rd  | imm    | 2    | B0..B3 | Z                    | Or immediate                               |
| SINGLE REGISTER LOGIC II   | ORA      | 1   | 0   | 1   | 1   | 0   | 1   | rd  | rd  | addr   | 2    | B4..B7 | Z                    | Or address                                 |
| SINGLE REGISTER LOGIC II   | XRI      | 1   | 0   | 1   | 1   | 1   | 0   | rd  | rd  | imm    | 2    | B8..BB | Z                    | Xor immediate                              |
| SINGLE REGISTER LOGIC II   | XRA      | 1   | 0   | 1   | 1   | 1   | 1   | rd  | rd  | addr   | 2    | BC..BF | Z                    | Xor address                                |
| OR                         | OR       | 1   | 1   | 0   | 0   | rs  | rs  | rd  | rd  | NA     | 1    | C0..CF | Z                    | Or                                         |
| XOR                        | XOR      | 1   | 1   | 0   | 1   | rs  | rs  | rd  | rd  | NA     | 1    | D0..DF | Z                    | Xor                                        |
| ATOMIC                     | INC      | 1   | 1   | 1   | 0   | 0   | 0   | rd  | rd  | NA     | 1    | E0..E3 | Z                    | Increment                                  |
| ATOMIC                     | DEC      | 1   | 1   | 1   | 0   | 0   | 1   | rd  | rd  | NA     | 1    | E4..E7 | Z                    | Decrement                                  |
| BIT TEST                   | BTI      | 1   | 1   | 1   | 0   | 1   | 0   | rd  | rd  | imm    | 2    | E8..EB | Z                    | Z = (rd & imm) == 0 ? 0 : 1                |
| BIT TEST                   | BTA      | 1   | 1   | 1   | 0   | 1   | 1   | rd  | rd  | addr   | 2    | EC..EF | Z                    | Z = (rd & addr) == 0 ? 0 : 1               |
| EXTENSION                  | EXT      | 1   | 1   | 1   | 1   | 0   | 0   | 0   | 0   | NA     | 2-3  | F0     |                      | To be followed by full 8bit opcode         |

The excel sheet ISA [here](assets/other/ISA.xlsx)

![[assets/images/cpu-simple-isa-rev-1.png]]