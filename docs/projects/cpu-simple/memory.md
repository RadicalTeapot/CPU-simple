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

| Address range | Use         | Size (in bytes) |
| ------------- | ----------- | --------------- |
| 0x00 - 0xEF   | Code + data | 240             |
| 0xF0 - 0xFF   | Stack       | 16              |
