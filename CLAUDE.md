# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Educational 8-bit CPU simulator in C# with a complete assembler toolchain, backend debugger, LSP language server, and Neovim IDE integration. Targets .NET 10.0.

## Coding Principles

These principles apply to all code changes:

1. **Think before coding** — State assumptions explicitly. If multiple interpretations exist, surface them. If something is unclear, ask rather than guess.
2. **Simplicity first** — Minimum code that solves the problem. No speculative features, abstractions for single-use code, or unasked-for flexibility.
3. **Surgical changes** — Touch only what is necessary. Match existing style. Don't refactor adjacent code. If you notice unrelated dead code, mention it but don't delete it. Remove only imports/variables/functions that *your* changes made unused.
4. **Goal-driven execution** — Transform tasks into verifiable goals. For multi-step tasks, state a brief plan with success criteria before starting.

## Code Guidelines

These guidelines apply to all languages used in this codebase (C#, lua):

Declare members in the following order: `public` first, then `protected`, then `private`. This applies to both types (classes, structs, enums) and their members (fields, properties, methods).

For `public` avoid fields, using properties instead. Exception: `const` fields go at the very top of the class. Order: const fields (if any) → properties → static builder / factory (if any) → constructor → methods
For `protected` avoid fields, using properties instead. Order: properties → methods
For `private` fields should be at the end of the class. Lock objects (e.g., for synchronization used in protected/private methods) are treated as private fields and belong in this block. Order: methods → const fields (if any) → lock objects (if any) → regular fields → readonly fields

If inheriting, `override` methods go last in their respective accessor blocks.

In case of doubt: arrange functions so that callers appear above callees. The most important/high-level concepts come first, details follow. Readers can stop reading once they have enough context (From Clean Code (Robert C. Martin))


## Build & Test Commands

```bash
# Build (8-bit, default)
dotnet build cpu-simple.sln -c Debug

# Build (16-bit address space)
dotnet build cpu-simple.sln -c DebugX16

# Run all tests
dotnet test cpu-simple.sln -c Debug

# Run a single test project
dotnet test CPU.Tests/CPU.Tests.csproj
dotnet test Assembler.Tests/Assembler.Tests.csproj
dotnet test Backend.Tests/Backend.Tests.csproj
dotnet test LanguageServer.Tests/LanguageServer.Tests.csproj

# Run a specific test by name
dotnet test CPU.Tests/CPU.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run the backend debugger
dotnet run --project Backend/Backend.csproj

# Run the assembler
dotnet run --project Assembler/Assembler.csproj

# Run the LSP language server (stdio)
dotnet run --project LanguageServer/LanguageServer.csproj

# Tree-sitter grammar (requires Node.js)
cd tree-sitter-grammar && npm install && npm run build
npm test  # run grammar tests
```

## Architecture

The system is a classic assembler-to-CPU pipeline:

```
Assembly source (.csasm) → Lexer → Parser → Analyser → Emitter → Machine code (bytes)
                                                                        ↓
                                                                  CPU (execute)
                                                                        ↓
                                                                  Backend (debugger, JSON over stdin/stdout)
                                                                        ↓
                                                                  Neovim plugin (IDE)
```

### C# Projects (in `cpu-simple.sln`)

- **CPU/** - Core CPU library: fetch-decode-execute cycle, memory, stack, registers, flags (Zero/Carry). The `OpcodeFactory` uses reflection to discover opcode classes annotated with `[Opcode]` attribute implementing `IOpcode`. Current implementation treats all instructions as single-cycle, but a realistic timing model is documented in `docs/projects/cpu-simple/micro-code.md` (Fetch cost = instruction bytes, Decode = 0 cycles [parallel with fetch], Execute = 0/1/2 cycles based on operation type).
- **Assembler/** - CLI tool with four pipeline stages: `Lexer` (tokenizes) → `Parser` (builds AST) → `Analyser` (two-pass: builds symbol table, resolves labels) → `Emitter` (produces bytes). Supports `.text` and `.data` sections.
- **Backend/** - Console debugger hosting the CPU. Reads JSON commands from stdin, writes JSON responses to stdout. Commands are discovered via `[Command]` attribute, split into `GlobalCommands/` (dump, breakpoint, status) and `StateCommands/` (load, run, step, pause, reset).
- **LanguageServer/** - LSP server for `.csasm` files (diagnostics, hover, completion). Uses the OmniSharp LSP SDK (`OmniSharp.Extensions.LanguageServer`) over stdio. Reuses the Assembler pipeline directly — runs Lexer→Parser→Analyser on each document change and translates exceptions into LSP diagnostics.
- **CPU.Tests/**, **Assembler.Tests/**, **Backend.Tests/**, and **LanguageServer.Tests/** - NUnit 4 test suites. Backend.Tests uses `InternalsVisibleTo` to access internal Backend types.

### Non-C# Components

- **nvim-plugin/** - Lua-based Neovim plugin providing `:Cpu*` commands and LSP auto-attach for `.csasm` files.
- **tree-sitter-grammar/** - Tree-sitter grammar for `.csasm` assembly syntax highlighting.

## Key Patterns

- **Opcode timing model**: `docs/projects/cpu-simple/micro-code.md` is the **authoritative reference** for opcode phase design. Every opcode must follow the tick rules defined there — each tick is either a single bus transaction (memory read/write, stack push/pop) OR a single internal operation (ALU, flag update, PC/EA update), never both. Bookkeeping (`PC++`, `SP±`) is allowed alongside a bus tick. Effective address calculations (indexed addressing) require their own internal tick. Refer to the doc when deciding how many phases an opcode needs and what `MicroPhase` value each should return.

- **8-bit vs 16-bit builds**: Controlled by `#if x16` conditional compilation. The `DebugX16`/`ReleaseX16` configurations define the `x16` symbol, which changes address sizes from 1 byte to 2 bytes throughout CPU and Assembler.
- **Attribute-based discovery**: Both opcodes (`[Opcode]` attribute + `IOpcode` interface) and backend commands (`[Command]` attribute + `ICommand` interface) use reflection-based registries to auto-discover implementations.
- **Opcode constructor signature**: All opcodes must have constructor `(State, Memory, Stack, OpcodeArgs)`.
- **Assembler debug output**: The assembler can emit a JSON debug file containing symbol table and span-to-address mappings for IDE integration.
- **Assembler exception patterns**: `Lexer` throws `LexerException` directly. `Parser.ParseProgram()` and `Analyser.Run()` collect errors and throw `AggregateException` wrapping `ParserException`/`AnalyserException` respectively. However, `Analyser.Run()` can also throw a bare `ParserException` from its `ResolveLabels()` phase (for unresolved label references). All assembler exceptions bake ` at line N, column M` into the message string and expose `Line`/`Column` properties (0-based).
- **Assembly syntax**: Memory address operands require square brackets (e.g., `jmp [label]`, `lda r0, [label]`, `ldx r1, [r0 + #0x01]`). Immediate values use `#` prefix (e.g., `ldi r0, #0x05`). The lexer lowercases all input and trims leading whitespace from each line before tokenizing but the original line number and column are preserved.
- **LSP SDK (OmniSharp)**: Uses `TextDocumentSelector` (not `DocumentSelector`) for handler registration. `CompletionHandlerBase` requires implementing both `Handle(CompletionParams, ...)` and `Handle(CompletionItem, ...)` (resolve). Markup content uses `MarkupKind.Markdown`.
- **CPU inspector from state commands**: Backend state commands can inspect CPU state via `CpuStateFactory.GetInspector()`. The `stepover` and `stepout` commands use this to read memory/stack and then delegate to `RunningState(ToAddress)` for breakpoint-aware execution.
- **Plugin source buffer targeting**: The plugin's `highlight_pc()` and `highlight_breakpoints()` functions use `assembler.assembler.last_source_bufnr` to target the correct source buffer instead of relying on `nvim_get_current_buf()`, which can return a sidebar panel buffer.

## Test Framework

NUnit 4 with `Microsoft.NET.Test.SDK`. Tests use `[Test]` and `[TestCase]` attributes.

### Test Helpers
- **Assembler tests**: `AnalyserTestsHelper.AnalyseProgram()` runs Lex→Parse→Analyse and returns emit nodes. `AnalyseAndEmit()` goes through the full pipeline to bytes. `GetSymbols()` returns the symbol table.
- **Backend tests**: `BackendTestHelpers.CreateGlobalContext()` creates a `GlobalCommandExecutionContext` with a real CPU and test doubles (`TestLogger`, `TestOutput`). Backend's `ParseArgs` is `internal` for direct testability.
