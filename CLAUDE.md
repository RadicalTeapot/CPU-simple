# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Educational 8-bit CPU simulator in C# with a complete assembler toolchain, backend debugger, and Neovim IDE integration. Targets .NET 10.0.

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

# Run a specific test by name
dotnet test CPU.Tests/CPU.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run the backend debugger
dotnet run --project Backend/Backend.csproj

# Run the assembler
dotnet run --project Assembler/Assembler.csproj

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

- **CPU/** - Core CPU library: fetch-decode-execute cycle, memory, stack, registers, flags (Zero/Carry). The `OpcodeFactory` uses reflection to discover opcode classes annotated with `[Opcode]` attribute implementing `IOpcode`.
- **Assembler/** - CLI tool with four pipeline stages: `Lexer` (tokenizes) → `Parser` (builds AST) → `Analyser` (two-pass: builds symbol table, resolves labels) → `Emitter` (produces bytes). Supports `.text` and `.data` sections.
- **Backend/** - Console debugger hosting the CPU. Reads JSON commands from stdin, writes JSON responses to stdout. Commands are discovered via `[Command]` attribute, split into `GlobalCommands/` (dump, breakpoint, status) and `StateCommands/` (load, run, step, pause, reset).
- **CPU.Tests/** and **Assembler.Tests/** - NUnit 4 test suites.

### Non-C# Components

- **nvim-plugin/** - Lua-based Neovim plugin providing `:Cpu*` commands.
- **tree-sitter-grammar/** - Tree-sitter grammar for `.csasm` assembly syntax highlighting.

## Key Patterns

- **8-bit vs 16-bit builds**: Controlled by `#if x16` conditional compilation. The `DebugX16`/`ReleaseX16` configurations define the `x16` symbol, which changes address sizes from 1 byte to 2 bytes throughout CPU and Assembler.
- **Attribute-based discovery**: Both opcodes (`[Opcode]` attribute + `IOpcode` interface) and backend commands (`[Command]` attribute + `ICommand` interface) use reflection-based registries to auto-discover implementations.
- **Opcode constructor signature**: All opcodes must have constructor `(State, Memory, Stack, OpcodeArgs)`.
- **Assembler debug output**: The assembler can emit a JSON debug file containing symbol table and span-to-address mappings for IDE integration.

## Test Framework

NUnit 4 with `Microsoft.NET.Test.SDK`. Tests use `[Test]` and `[TestCase]` attributes.
