# Tree-sitter Grammar for CPU-simple Assembly

This document explains how the tree-sitter grammar in `tree-sitter-grammar/` implements the EBNF specification from `assembly.md`.

## EBNF to Tree-sitter Rule Mapping

| EBNF Production | Tree-sitter Rule | Notes |
|-----------------|------------------|-------|
| (file) | `source_file` | Root: sequence of `line` nodes |
| (line) | `line` | Optional statement + newline |
| `statement` | `statement` | At least one component required |
| `directive` (section) | `header_directive` | `.text`, `.data` separated for semantics |
| `directive` (data) | `directive` | `.byte`, `.short`, `.zero`, `.org`, `.string` |
| `label` | `label_definition` | `identifier :` |
| `instruction` | `instruction` | `mnemonic [operand [, operand]]` |
| `comment` | `comment` | `;` to end of line |
| `reg` | `register` | `/[rR][0-9]+/` |
| `identifier` | `identifier` | `/[a-zA-Z_][a-zA-Z0-9_]*/` |
| `hex-alphanum` | `hex_number` | `/0[xX][0-9a-fA-F]+/` |
| `num-literal` | `immediate_value` | `# hex_number` |
| `memory` | `memory_address` | `[ memory_operand ]` |
| `memory-identifier` | `memory_operand` | All 6 addressing modes |
| `string-literal` | `string_literal` | Supports `\"` and `\\` escapes |
| `argument` | `operand` | Register, immediate, identifier, or memory |

## Case Insensitivity

The CPU-simple assembly language is case insensitive. This is handled through regex character classes:

- **Registers**: `/[rR][0-9]+/` matches `R0`, `r0`, `R15`, etc.
- **Hex numbers**: `/0[xX][0-9a-fA-F]+/` matches `0x`, `0X`, and any hex digit case
- **Directives**: Each directive uses case-insensitive character classes
  - `.text` = `/[tT][eE][xX][tT]/`
  - `.data` = `/[dD][aA][tT][aA]/`
  - `.byte` = `/[bB][yY][tT][eE]/`
  - etc.
- **Identifiers/Mnemonics**: `/[a-zA-Z_][a-zA-Z0-9_]*/` accepts mixed case

## Token Priority

Tree-sitter uses precedence to resolve ambiguities between overlapping patterns:

```javascript
register: ($) => prec(2, /[rR][0-9]+/)
identifier: ($) => /[a-zA-Z_][a-zA-Z0-9_]*/  // default priority
```

This ensures `R0` is always parsed as a register, not an identifier, matching the existing assembler's lexer behavior (see `Assembler/Lexeme/RegisterLexeme.cs:3`).

## Memory Addressing Modes

The grammar supports all 6 memory addressing modes from the specification:

| Mode | Example | Grammar Match |
|------|---------|---------------|
| Direct hex | `[#0x0C]` | `immediate_value` |
| Label | `[data]` | `identifier` |
| Label + offset | `[data+#0x05]` | `identifier "+" immediate_value` |
| Label - offset | `[data-#0x05]` | `identifier "-" immediate_value` |
| Register | `[R1]` | `register` |
| Register + offset | `[R1+#0x01]` | `register "+" immediate_value` |

The order of alternatives in `memory_operand` matters - more specific patterns (with offsets) are listed before simpler patterns to ensure correct parsing.

## Whitespace Handling

The grammar allows whitespace between symbols as specified:

```javascript
extras: ($) => [/[ \t]+/]
```

This means spaces and tabs are automatically skipped everywhere except:
- Inside string literals
- Inside comments
- Newlines (which separate statements)

Examples of valid syntax:
- `# 0x05` - whitespace between `#` and hex number
- `abc :` - whitespace before colon
- `. text` - whitespace after dot in directive

## Node Types for Syntax Highlighting

The `queries/highlights.scm` file maps grammar nodes to highlight groups:

| Node Type | Highlight Group | Color Meaning |
|-----------|-----------------|---------------|
| `comment` | `@comment` | Comments (typically gray/green) |
| `register` | `@variable.builtin` | Built-in variables (typically cyan) |
| `directive_name` | `@keyword.directive` | Preprocessor directives |
| `header_directive` | `@keyword.directive` | Section directives |
| `mnemonic` | `@function` | Function/instruction names |
| `hex_number` | `@number` | Numeric literals |
| `string_literal` | `@string` | String literals |
| `label_definition > identifier` | `@label` | Label definitions |
| `operand > identifier` | `@variable` | Label references |

## Building and Testing

```bash
cd tree-sitter-grammar

# Install dependencies
npm install

# Generate parser
npm run build

# Run tests
npm test

# Parse a file
npx tree-sitter parse ../tests/prog-1.csasm

# Test highlighting
npx tree-sitter highlight ../tests/prog-1.csasm
```

## File Structure

```
tree-sitter-grammar/
├── grammar.js           # Main grammar definition
├── package.json         # NPM configuration
├── queries/
│   └── highlights.scm   # Syntax highlighting queries
└── test/
    └── corpus/          # Test cases
        ├── statements.txt
        ├── directives.txt
        ├── instructions.txt
        ├── memory.txt
        ├── comments.txt
        └── edge_cases.txt
```
