# CPU Simple

A minimal educational 8-bit CPU implemented in C# with a small runtime, opcode set, and tests. This repository contains the CPU core, an opcode table, a test suite, and a simple console entry point.

## Repository Structure

- `CPU/`: Core CPU implementation (state, stack, trace, opcodes integration)
- `opcodes/`: Opcode abstractions, table, and exceptions
- `CPU.Tests/`: Unit tests for CPU and opcodes
- `Main/`: Console application that hosts/runs the CPU
- `docs/`: Design and specification documents

## Documentation

Refer to the following documents for design details and the ISA:

- `docs/CPU spec.md`: Full CPU specification and behavior
- `docs/Opcode map.md`: Opcode list, encoding, and semantics
- `docs/Design phases.md`: Development phases and architectural notes

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

- Simple, readable CPU implementation for learning and experimentation
- Clear opcode definitions and mapping
- Test-driven verification of CPU behavior