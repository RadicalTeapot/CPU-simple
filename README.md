# CPU Simple

A minimal educational 8-bit CPU implemented in C# with a small runtime, opcode set, and tests. 
This repository contains the CPU core, a backend server acting a simple debugger, a test suite, a compiler and a Neovim plugin to serve as the IDE.

## Repository Structure

- `CPU/`: Core CPU implementation
- `CPU.Tests/`: Unit tests for CPU and opcodes
- `Assembler/`: Compiler to convert assembly files into machine code
- `Assembler.Tests/` : Unit test for the assembler code
- `Backend/`: Console application that hosts/runs the CPU to be used for debugging
- `nvim-plugin/`: Neovim plugin to serve as the IDE
- `docs/`: Design and specification documents

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

## Neovim as IDE

### Setup

Copy the content of `nvim-plugin` folder into your neovim config folder and require the plugin.

### Config

```lua
require("cpu-simple").setup({
  backend_path = "/path/to/Backend.exe",
  assembler_path = "/path/to/Assembler.exe",
  memory_size = 256,
  stack_size = 16,
  registers = 4,
})
```

### Commands

- `:CpuStart`: Start the CPU backend process
- `:CpuStop`: Stop the CPU backend process
- `:CpuAssemble`: Assemble the current buffer to machine code
- `:CpuLoad`: Load machine code into the CPU
- `:CpuRun`: Run the loaded program
- `:CpuSend`: Send a raw command to the CPU backend
- `:CpuDump`: Dump CPU state, memory and stack contents

## Project Goals

- Simple, readable CPU and assembler implementation for learning and experimentation
- Clear opcode definitions and mapping
- Simple and useable assembly language
- Test-driven verification of CPU and assembler behavior

## AI use disclamer

I used Github copilot to help brainstorm the general architecture and write some unit tests, the vast majority of the functional code was written by hand.
The notable exception for this is the Neovim plugin, which is written almost entirely by AI as this is far from my area of expertise.
I did review the code but exercise caution when using it.

## To do (in no particular order)

- [/] Write a bank of small programs for 8 and 16 bit version
  - [ ] Test out stack operations
- [ ] Missing assembler unit tests
  - [ ] Analyser
  - [ ] Emitter
  - [ ] Backend
- [/] Change the CPU program to act as a backend that listens on STDIN for commands (load_prog(file_path), step(n=1), run(until...), read_mem) and sends back structured responses on STDOUT (lua tables)
  - [x] Commands via STDIN
    - [x] load
    - [x] step
    - [x] run
    - [x] read_mem
  - [x] Logging on STDERR
  - [x] Add memory and stack diffs to CpuInspector
- [/] Implement CPU IDE ([inspiration for some UI](https://github.com/AfaanBilal/NanoCore/blob/master/assets/NanoCoreTUI.gif)) in Neovim
  - [x] Run backend in Lua and communication over STDIN, STDOUT
  - [x] `:CpuLoad` (load on CPU and reset)
  - [x] `:CpuStep`, `:CpuRun` and `:CpuReset` with simple print of status to a status line
  - [x] `:CpuDump` full memory, stack and status dump to a scratch buffer
  - [x] Highlight (using virtual text) of source -> machine code
  - [ ] Nicer CPU dump that auto-updates on step / run (split in 3 panels: status, memory, stack)
  - [ ] `:CpuToggletBp address`, `:CpuToggleBp` (set on cursor) (and virtual text highlight for BP position in source / machine code)
  - [ ] `:CpuToggleBP symbol` (and virtual text highlight for BP position in source / machine code)
  - [ ] `:StepOver`, `:StepIn`, `:StepOut`
  - [ ] `:CpuRunToCursor`
  - [ ] Cleanup buffer UIs
  - [ ] Handle assembler errors
- [ ] Write grammar for tree-sitter
- [ ] Write LSP server
- [ ] Implement PPU and map some memory for it (for 16-bit version)
- [ ] Implement sound chip and map some memory for it too (for 16-bit version)
  - [ ] Write a small MIDI player application
- [ ] Cleanup opcode constructors to take only necessary parameters (or use an interface to mask un-necessary parameters)
- [/] Cleanup docs and document design choices