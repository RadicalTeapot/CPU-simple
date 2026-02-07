# Language Server Protocol (LSP) for `.csasm`

The LanguageServer project provides language intelligence for `.csasm` assembly files. It implements the LSP specification over stdio, enabling any LSP-compatible editor to provide diagnostics, hover documentation, and autocompletion.

## Architecture

The LSP server is a C# console application that communicates via JSON-RPC over stdin/stdout, using the [OmniSharp LSP SDK](https://github.com/OmniSharp/csharp-language-server-protocol). It reuses the existing assembler pipeline (Lexer, Parser, Analyser) to analyse source files without any modifications to the assembler project.

```
Editor (Neovim, VS Code, etc.)
  ↕  JSON-RPC over stdio
LanguageServer
  ├── DocumentStore          — tracks open documents and cached analysis results
  ├── DocumentAnalyser       — runs Lexer → Parser → Analyser pipeline
  ├── TokenLocator           — maps cursor position to token
  └── Handlers/
      ├── TextDocumentSyncHandler  — publishes diagnostics on open/change/close
      ├── HoverHandler             — instruction/register/label documentation
      └── CompletionHandler        — context-aware completions
```

## How It Works

### Document Analysis Pipeline

When a document is opened or changed, `DocumentAnalyser` runs the assembler pipeline in three stages:

1. **Lexer** (`Assembler.Lexer.Tokenize`) — tokenizes the source text into a list of `Token` structs. If the lexer fails, a single diagnostic is produced and later stages are skipped.

2. **Parser** (`Assembler.Parser.ParseProgram`) — builds an AST (`ProgramNode`) from the token stream. Parser errors are collected (the parser attempts recovery by skipping to the next line) and reported as diagnostics. Tokens remain available even if parsing fails.

3. **Analyser** (`Assembler.Analyser.Run` + `GetSymbols`) — performs two-pass analysis: builds a symbol table and resolves labels. Analysis errors are reported as diagnostics. The AST and tokens remain available even if analysis fails, enabling hover and completion to work with partial results.

Each stage's exceptions are caught independently, and their messages are converted to LSP `Diagnostic` objects. The ` at line N, column M` suffix that the assembler bakes into error messages is stripped since the LSP diagnostic already carries position information.

### Column Offset Adjustment

The assembler's Lexer trims leading whitespace from each line before tokenizing (`Lexer.GetCleanedLinesFromSource` calls `.Trim()`). This means token column values are 0-based relative to the **trimmed** line, not the original source. For example, `   INC R0` (3 leading spaces) produces `INC` at column 0 instead of column 3.

Since LSP clients send cursor positions relative to the original source text, `DocumentAnalyser` compensates immediately after lexing: it computes the leading whitespace count for each line and creates adjusted tokens with `column + offset`. Because all downstream positions — AST node spans, Parser exception columns, Analyser exception columns — derive from token column values, this single adjustment at the token level corrects positions throughout the entire pipeline (hover ranges, completion context, and diagnostic positions).

For `LexerException` (thrown before tokens are available), the offset is computed and applied directly to the diagnostic column.

### Token Location

`TokenLocator` maps a cursor position `(line, column)` to the token at that position by scanning the token list. It also provides context helpers:

- `IsDirectiveContext` — the token follows a `.` (dot)
- `IsInstructionMnemonic` — the token is the first identifier on its line (after any label definition)
- `IsLabelDefinition` — the token is followed by a `:`
- `GetMnemonicForLine` — finds the instruction mnemonic on a given line

These helpers are used by the hover and completion handlers to determine what kind of information to provide.

### Diagnostics

Diagnostics are published via `textDocument/publishDiagnostics` whenever a document is opened or changed. They map directly from assembler exception positions (0-based line and column, matching LSP conventions). Closing a document clears its diagnostics.

### Hover

The hover handler identifies the token under the cursor and returns markdown documentation:

| Token Type | Context | Hover Content |
|---|---|---|
| Identifier | After `.` | Directive description and syntax |
| Identifier | First on line, valid mnemonic | Instruction description and syntax |
| Identifier | Label definition/reference | Symbol address and kind |
| Register | Any | Register description |
| HexNumber | Any | Decimal equivalent |

Instruction and directive descriptions are maintained in static dictionaries (`InstructionDescriptions` and `DirectiveDescriptions`) covering all 48 opcodes and 7 directives.

### Completion

The completion handler provides context-aware suggestions based on cursor position:

| Context | Offered Items |
|---|---|
| Line start (empty or first word) | All instruction mnemonics + `.text`/`.data` section directives |
| After `.` (trigger character) | Directive names: `text`, `data`, `byte`, `short`, `zero`, `org`, `string` |
| Operand position | Context-sensitive based on the instruction's operand type |

Operand completion uses the same instruction-to-operand-type grouping as the analyser:

- **No operand**: `nop`, `hlt`, `clc`, `sec`, `clz`, `sez`, `ret`
- **Single memory address** (labels): `jmp`, `jcc`, `jcs`, `jzc`, `jzs`, `cal`
- **Single register**: `pop`, `pek`, `psh`, `lsh`, `rsh`, `lrt`, `rrt`, `inc`, `dec`
- **Register + immediate**: `ldi`, `adi`, `sbi`, `cpi`, `ani`, `ori`, `xri`, `bti`
- **Register + memory address**: `lda`, `sta`, `ada`, `sba`, `cpa`, `ana`, `ora`, `xra`, `bta`, `ldx`, `stx`
- **Two registers**: `mov`, `add`, `sub`, `cmp`, `and`, `or`, `xor`

## Building

```bash
dotnet build LanguageServer/LanguageServer.csproj -c Debug
```

The built executable is at `LanguageServer/bin/Debug/net10.0/LanguageServer`.

## Neovim Integration

The Neovim plugin (`nvim-plugin/`) auto-detects and starts the LSP server when a `.csasm` file is opened. The `lsp_path` configuration option specifies the path to the server executable:

```lua
cpu_simple.setup({
    lsp_path = "path/to/LanguageServer",
    -- ... other options
})
```

The plugin searches common build output paths automatically (same pattern as `backend_path` and `assembler_path`). When configured, a `FileType` autocmd for `csasm` calls `vim.lsp.start()` to attach the server.

Verify the LSP is attached with `:LspInfo` or `:checkhealth lsp`.

## Testing

```bash
dotnet test LanguageServer.Tests/LanguageServer.Tests.csproj -c Debug
```

The test suite covers:

- **DocumentAnalyserTests** — verifies pipeline stages produce correct diagnostics and preserve partial results on error
- **TokenLocatorTests** — verifies cursor-to-token mapping and context detection helpers

## Project Structure

```
LanguageServer/
├── LanguageServer.csproj
├── Program.cs                          — OmniSharp server bootstrap (stdio)
├── DocumentStore.cs                    — open document tracking with analysis cache
├── DocumentAnalyser.cs                 — assembler pipeline runner
├── TokenLocator.cs                     — cursor position to token mapping
├── InstructionDescriptions.cs          — mnemonic → (description, syntax) dictionary
├── DirectiveDescriptions.cs            — directive → (description, syntax) dictionary
└── Handlers/
    ├── TextDocumentSyncHandler.cs      — document sync + diagnostics
    ├── HoverHandler.cs                 — hover documentation
    └── CompletionHandler.cs            — context-aware completion
```
