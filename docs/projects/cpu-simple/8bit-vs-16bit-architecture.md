# 8bit vs 16bit architecture

## Implementation

Using compile flags to let the compiler know without having to make everything generic.
The main change was to split actions on memory addresses (which might be 16bit wide) from other ones (e.g., using immediates
or registers).

16bit is little-endian.

### Stack

Introduced `PushAddress`, `PopAddress` and `PeekAddress` that map to either the `byte` or `ushort` matching methods.

### Memory

`ReadByte` and `WriteByte` take either a `byte` or `ushort` address and introduced `ReadAddress` and `WriteAddress` that
return the correct type.

### State

Change `PC` to be either a `ushort` or `byte` register.