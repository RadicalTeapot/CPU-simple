# Assembly

## EBNF syntax

```EBNF
num = ? [0-9] ?;
hex-alpha = ? [A-Fa-f] ?;
alpha = ? [a-zA-Z] ?;
all-chars = ? all visible characters ?; (* Only ASCII characters, tab character included *)
string-chars = ? all visible characters with escaped chars ?; (* Only ASCII, tab character included, double quote and backward-slash characters escaped with backwards-slash *)

hex-alphanum = '0x', ( num | hex-alpha ), { num | hex-alpha };
num-literal = '#', hex-alphanum;
reg = 'r', num, { num };
identifier = ( '_' | alpha ), { num | alpha | '_' };
memory-identifier = num-literal | identifier | reg | ( identifier, ( + | - ), num-literal ) | (reg, (+ | -), num-litteral );
memory = '[', memory-identifier, ']';
argument = reg | num-literal | identifier | memory;
string-literal = '"', { string-chars }, '"';

label = identifier, ':';
directive = '.', identifier, [ ( num-literal, [ ',', num-literal ] ) | string-literal ];
instruction = identifier, [ argument, [ ',', argument ] ];
comment = ';', { all-chars };

statement = [ directive ], [ label ], [ directive | instruction ], [ comment ];
```

## Language specification

- Language is case insensitive
- Comments are supported using the `;` character
- Labels are formed by specifying the label name followed by a colon syntax (e.g., `label:`)
- Immediate values are prefixed with a `#` (e.g., `adi r0, #0x05`)
- Labels can be on their own line or inlined with directives / instructions.
- For now only hexadecimal numbers are supported (prefixed with `0x` to distinguish them from identifiers)
- Valid first character for identifiers are `[a-zA-Z]` and `_`

### Memory

Memory addresses are surrounded by square brackets (e.g., `ada r0, [#0x0C]`), supported addresses are:

- Direct values in hexadecimal notation, prefixed with `#` (`ada r0, [#0x0C]`)
- Using labels (`ada r0, [label]`)
- Offset values using the `+` or `-` symbol (`ada r0, [label+#0x05]`)

### Directives

Section directives are only semantically valid as the first directive of a statement. There are currently two section directives:

- `.text` defines the program section. It always starts to address `0x00`. Muliple sections are concatenated together when emitting machine code.
- `.data` defines the data declaration section, typically only one such section is declared. Those are emitted in the same order as declared in program, starting right after `.text` section ends.

The following directives are syntactically valid anywhere, but semantically restricted to data section:
- `.org <addr>, <fill>` move the location counter to `<addr>` and fill the gaps with `<fill>` (If `<fill>` is omitted, the assembler fills gaps with `#00`). Both `addr` and `fill` are byte literals.
- `.byte` indicates a single byte
- `.short` indicates a 2 bytes value
- `.zero` indicates a number of bytes set to zero (e.g., `.zero #0A` will create 10 null bytes)
- `.string` declares a sequence of bytes followed by a null byte (e.g. `.string "hello"` creates 6 bytes, one for each character and a zero byte), ASCII encoded. Supported escapes are `\"` and `\\`.

## General notes

- If an instruction uses two registers, the template is INSTRUCTION DESTINATION, SOURCE (note that this is the opposite of how the bits are ordered in emitted code and is done to keep with best practices), e.g.,
    - `mov rd, rs ; rd <- rs`
    - `add rd, rs ; rd ← rd + rs`
- Operands are comma separated
- If running using an 8bit address space, addressable memory is `0x00` to `0xEF`. Any address outside that range will result in an error at compile time. (`0xF0–0xFF` are reserved for the stack)
- CPU endianness is little-endian.
- Allocating to already written addresses (e.g., by jumping backwards) using `.org` will result in an error at compile time
- While it is syntactically allowed to use a label to set `.org` arguments, it is semantically restricted to only byte literals 
- Signed numbers are not supported yet
- Whitespace are allowed between symbols and identifiers / numbers, e.g,
    - `# 05` is a valid hex literal
    - `abc :` is a valid label
    - `. text` is a valid directive