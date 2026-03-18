# Memory

Since `PC` is 1 byte wide the size of the addressable memory is 256 bytes.

This could be extended using pages or swapping PC to be 2 bytes but is not planned for now.

## Registers

The [registers](projects/cpu-simple/registers) (`PC`, `SP`, `flags` and the 4 `GPR`) and not part of the addressable memory space.

## Stack

The last 16 bytes of the memory is reserved for the stack, this is not memory mapped (i.e. `PC` max value is `0xEF`).

## Program data

Program data starts at `0x00`

## Layout

| Address range | Use                        | Size (in bytes) |
| ------------- | -------------------------- | --------------- |
| 0x00 – 0xE6   | Code + data + IRQ handler  | 231             |
| 0xE7          | IRQ vector table           | 1               |
| 0xE8 – 0xEF   | MMIO                       | 8               |
| 0xF0 – 0xFF   | Stack                      | 16              |

The IRQ vector table holds the address of the interrupt handler. Code, data, and the handler itself are placed sequentially starting at `0x00`; the assembler emits the vector table entry at `0xE7` pointing to wherever the handler ended up. See [vector-table.md](vector-table.md) for details.
