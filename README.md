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
- `tree-sitter-grammar/`: Tree sitter grammar generator for the assembly language
- `docs/`: Design and specification documents (including instruction timing model in `docs/projects/cpu-simple/micro-code.md`)

## Architectures

The CPU can be built using either an 8bit address space (`Debug` and `Release` configurations) or a 16bit address space (`DebugX16` and `ReleaseX16` configurations).
This is achieved by setting the `x16` compile flag for 16-bit build.

## Getting Started

### Prerequisites

- .NET SDK 10.0 or newer installed
- Node and npm (to build tree sitter grammar)
- C compiler (to run tree sitter grammar tests)

### Build and Test

Use the solution file `cpu-simple.sln` to build and run tests.

```pwsh
# From the repository root
dotnet restore
dotnet build cpu-simple.sln -c Debug

# Run tests
dotnet test cpu-simple.sln -c Debug
```

### Run the backend

```pwsh
# Run the Main project
dotnet run --project Backend/Backend.csproj
```

### Generate the treesitter grammar

```bash
cd tree-sitter-grammar
npm install
npm run build
```

#### Testing

```bash
# Run tests
npm test
# Parse a file
npx tree-sitter parse tests/prog-1.csasm
# Test highlighting
npx tree-sitter highlight tests/prog-1.csasm
```

#### Troubleshooting

See [Neovim plugin troubleshoot section](docs/projects/cpu-simple/neovim-plugin.md#troubleshooting)

## Neovim as IDE

### Setup

Copy the content of `nvim-plugin` folder into your neovim config folder and require the plugin.

Run the Neovim plugin smoke test:

```bash
./nvim-plugin/smoke.sh
```

Run the Neovim plugin integration tests:

```bash
./nvim-plugin/tests/run.sh
```

### Config

```lua
require("cpu-simple").setup({
  backend_path = "/path/to/Backend.exe",
  assembler_path = "/path/to/Assembler.exe",
  memory_size = 256,
  stack_size = 16,
  registers = 4,
  sidebar = {
    panels = {
      memory = {
        changed_highlight = {
          enabled = true,
          timeout_ms = 1500, -- 0 = persistent
        },
        cursor_address_highlight = true,
      },
    },
  },
  source_annotations = {
    pc_operands_virtual_text = {
      enabled = true,
    },
  },
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

I used Claude to help brainstorm the general architecture and write some unit tests, the vast majority of the functional C# code was written by hand.

The notable exception for are:

- the Neovim plugin
- the tree sitter grammar
- the LSP server

Those where written almost entirely by Claude as they are far from my area of expertise.

I did review and tested the code but exercise caution when using it.

## To do (in no particular order)

- [ ] Change 16bit implementation so that computing the effective address is an internal ALU operation
- [ ] Implement CPU IDE ([inspiration for some UI](https://github.com/AfaanBilal/NanoCore/blob/master/assets/NanoCoreTUI.gif)) in Neovim
  - [x] Fix issue when not all json is read in one go in neovim
  - [ ] Add command to swap between step and tick mode (to backend and nvim-plugin)
  - [ ] When in tick mode, allow to set breakpoints on specific tick types (memory read, memory write,...)
  - [ ] Add a tick info panel that reports on the executed tick
  - [ ] Display mode (step or tick) in UI
  - [ ] When assembled, if sidebar was never opened, open the configured panels, otherwise just re-open sidebar
  - [ ] Test if assembler errors are handled
  - [ ] Error when loading CPU dump of 16bit version
- [ ] Implement PPU and map some memory for it (for 16-bit version)
  - [ ] This will necessitate interrupts
- [ ] Implement sound chip and map some memory for it too (for 16-bit version)
  - [ ] Write a small MIDI player application
- [ ] Cleanup opcode constructors to take only necessary parameters (or use an interface to mask un-necessary parameters)
- [ ] Write a bank of small programs for 8 and 16 bit version and document them
- [/] Cleanup docs and document design choices
