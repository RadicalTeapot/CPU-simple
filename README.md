# CPU Simple

A minimal educational 8-bit CPU implemented in C# with a small runtime, opcode set, and tests. This repository contains the CPU core, an opcode table, a test suite, a simple console entry point and a terminal UI client to visualize the CPU state.

## Repository Structure

- `CPU/CPU.cs`: Core CPU implementation
- `CPU/opcodes/`: Opcode abstractions, table, and exceptions
- `CPU/components/`: Memory, Stack and State
- `CPU.Tests/`: Unit tests for CPU and opcodes
- `Assembler/`: Convert assembly files into machine code
- `Assembler.Tests/` : Unit test for the assembler code
- `Main/`: Console application that hosts/runs the CPU
- `TUI/`: Console application to visualize the CPU state while running a program
- `docs/`: Design and specification documents

## Documentation

Documentation can be found in the `docs` folder.

## Architectures

The CPU can be built using either an 8bit address space (`Debug` and `Release` configurations) or a 16bit address space (`DebugX16` and `ReleaseX16` configurations).
This is achieved by setting the `x16` compile flag for 16-bit build.

## Getting Started

### Prerequisites

- .NET SDK 8.0 or newer installed

### Build and Test

Use the solution file `cpu-simple.sln` to build and run tests.

```pwsh
# From the repository root
dotnet restore
dotnet build cpu-simple.sln -c Debug

# Run tests
dotnet test cpu-simple.sln -c Debug
```

### Run the Console App

```pwsh
# Run the Main project
dotnet run --project Main/Main.csproj
```

## Project Goals

- Simple, readable CPU and assembler implementation for learning and experimentation
- Clear opcode definitions and mapping
- Simple and useable assembly language
- Test-driven verification of CPU and assembler behavior

## To do (in no particular order)

- [/] Write a bank of small programs for 8 and 16 bit version
  - [ ] Test out stack operations
- [/] Implement CPU TUI
  - [ ] Hook up remaining TUI panels
  - [ ] Run on test program bank
- [ ] Write grammar for tree-sitter
- [ ] Write LSP server
- [ ] Implement PPU and map some memory for it (for 16-bit version)
- [ ] Implement sound chip and map some memory for it too (for 16-bit version)
  - [ ] Write a small MIDI player application
- [ ] Cleanup opcode constructors to take only necessary parameters (or use an interface to mask un-necessary parameters)
- [/] Cleanup docs and document design choices