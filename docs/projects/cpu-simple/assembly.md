# Assembly

## Language specification

- Language is case insensitive
- Comments are supported using the `;` character
- Labels are formed by specifing the label name followed by a colon syntax (e.g., `label:`)
- Immediate values are prefixed with a `#` (e.g., `adi r0, #05`), their values are always expressed in hexadecimal format
- Memory addresses are surrounded by square brackets (e.g., `ada r0, [#0c]`), supported addresses are:
    - Direct values in hexadecimal notation, prefixed with `#` (`ada r0, [#0c]`)
    - Using labels (`ada r0, [label]`)
    - Offset values using the `+` or `-` symbol (`ada r0, [label+#05]`)
- Supported directives:
    - `.text` defines the program section (only one such section is supported) (always set to address `0x00`)
    - `.data` defines the data declaration section (placed one after the other in same order as declared in program, right after `.text` section)
- The following directives are only valid inside of `.data`:
    - `.org <addr>, <fill>` move the location counter to `<addr>` and fill the gaps with `<fill>` (`0x00` if not set). Both `addr` and `fill` are byte litterals.
    - `.byte` indicates a single byte
    - `.short` indicates a 2 bytes value
    - `.zero` indicates the a number of bytes set to zero (e.g., `.zero 10` will create 10 null bytes)
    - `.string` declares a sequence of bytes followed by a null byte (e.g. `.string "hello"` creates 6 bytes, one for each character and a zero byte), ASCII encoded. Supported escapes are `\"` and `\0`.

## General notes

- If an instruction uses two register, the template is INSTRUCTION DESTINATION, SOURCE (note that this is the opposite how the bits are ordered in emitted code and is done to keep with best practices), e.g.,
    - `mov rd, rs ; rd <- rs`
- Operands are comma separated
- If running using an 8bit address space, addressable memory is `0x00` to `0xEF`. Any address outside that range will result in an error at compile time. (`0xF0–0xFF` are reserved for the stack)
- CPU endianness is litte-endian.
- Allocating already used space (i.e., jumping backwards) using `.org` will result in an error at compile time
- Signed numbers are not supported yet

## Memory addressing syntax

`<hex> := # [0-9A-Fa-f]+`

`<expr> := <hex> | <label> | <label> ( + | - ) <hex>`

`<mem> := [ <expr> ]`