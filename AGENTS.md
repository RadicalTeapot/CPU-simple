# AGENTS.md

Guidance for agents working in this repository.

## Project Summary

Educational 8-bit CPU simulator in C# with assembler toolchain, backend debugger, LSP language server, and Neovim plugin. Targets .NET 10.0.

## Quick Commands

```bash
# Build (8-bit, default)
dotnet build cpu-simple.sln -c Debug

# Build (16-bit address space)
dotnet build cpu-simple.sln -c DebugX16

# Run all tests
dotnet test cpu-simple.sln -c Debug

# Run single test projects
dotnet test CPU.Tests/CPU.Tests.csproj
dotnet test Assembler.Tests/Assembler.Tests.csproj
dotnet test Backend.Tests/Backend.Tests.csproj
dotnet test LanguageServer.Tests/LanguageServer.Tests.csproj

# Run a specific test by name
dotnet test CPU.Tests/CPU.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run tools
dotnet run --project Backend/Backend.csproj
dotnet run --project Assembler/Assembler.csproj
dotnet run --project LanguageServer/LanguageServer.csproj

# Tree-sitter grammar (requires Node.js)
cd tree-sitter-grammar && npm install && npm run build
npm test
```

## Architecture

Pipeline: `Assembly (.csasm)` -> `Lexer` -> `Parser` -> `Analyser` -> `Emitter` -> machine code bytes -> `CPU` -> `Backend` (JSON over stdin/stdout) -> `Neovim plugin`.

Projects in `cpu-simple.sln`:

- `CPU/`: Core CPU library. `OpcodeFactory` uses reflection to discover `[Opcode]` classes implementing `IOpcode`.
- `Assembler/`: CLI tool with Lexer -> Parser -> Analyser (two-pass) -> Emitter. Supports `.text` and `.data`.
- `Backend/`: Debugger host. Commands discovered via `[Command]` and split into `GlobalCommands/` and `StateCommands/`.
- `LanguageServer/`: LSP server for `.csasm` using OmniSharp LSP SDK. Reuses Assembler pipeline and converts exceptions to diagnostics.
- `*.Tests/`: NUnit 4 test suites. Backend.Tests uses `InternalsVisibleTo`.

Non-C#:

- `nvim-plugin/`: Lua-based Neovim integration with `:Cpu*` commands and LSP auto-attach.
- `tree-sitter-grammar/`: Tree-sitter grammar for `.csasm` syntax highlighting.

## Key Patterns

- 8-bit vs 16-bit builds: `#if x16` gated by `DebugX16`/`ReleaseX16` configurations.
- Attribute discovery: opcodes via `[Opcode]`, backend commands via `[Command]`, both using reflection.
- Opcode constructor signature: `(State, Memory, Stack, OpcodeArgs)`.
- Assembler exceptions: `LexerException` thrown directly; `Parser.ParseProgram()` and `Analyser.Run()` may throw `AggregateException` of `ParserException`/`AnalyserException`. `Analyser.Run()` can also throw a bare `ParserException` during label resolution. Exceptions include `Line`/`Column` (0-based) and message text `at line N, column M`.
- Assembly syntax:
  - Memory operands require brackets: `jmp [label]`, `lda r0, [label]`, `ldx r1, [r0 + #0x01]`.
  - Immediate values use `#`: `ldi r0, #0x05`.
  - Lexer lowercases input and trims leading whitespace, but line/column are preserved.
- LSP SDK: uses `TextDocumentSelector`; completion requires both `Handle(CompletionParams, ...)` and resolve `Handle(CompletionItem, ...)`. Markup uses `MarkupKind.Markdown`.
- Backend stepping: `stepover`/`stepout` use `CpuStateFactory.GetInspector()` to read state and delegate to `RunningState(ToAddress)` for breakpoint-aware execution.
- Neovim plugin: `highlight_pc()` and `highlight_breakpoints()` target `assembler.assembler.last_source_bufnr` (not current buffer).

## Test Helpers

- Assembler: `AnalyserTestsHelper.AnalyseProgram()` and `AnalyseAndEmit()` cover pipeline; `GetSymbols()` returns symbol table.
- Backend: `BackendTestHelpers.CreateGlobalContext()` builds a real CPU with test doubles. `ParseArgs` is `internal` for tests.
