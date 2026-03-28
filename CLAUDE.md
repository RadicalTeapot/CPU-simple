# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Educational 8-bit CPU simulator in C# with a complete assembler toolchain, emulator debugger, LSP language server, and Neovim IDE integration. Targets .NET 10.0.

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
dotnet test Emulator.Tests/Emulator.Tests.csproj
dotnet test LanguageServer.Tests/LanguageServer.Tests.csproj

# Run a specific test by name
dotnet test CPU.Tests/CPU.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run the emulator debugger (headless, 10Hz)
dotnet run --project Emulator/Emulator.csproj

# Run the emulator with PPU rendering window (60fps, default scale 4×)
dotnet run --project Emulator/Emulator.csproj -- --vram 256

# Run with custom scale
dotnet run --project Emulator/Emulator.csproj -- --vram 256 --scale 2

# Run with a CHR ROM file (tile pattern data loaded from disk)
dotnet run --project Emulator/Emulator.csproj -- --vram 256 --chr /path/to/tiles.chr

# Run with controller input enabled (2 buttons; requires --vram)
dotnet run --project Emulator/Emulator.csproj -- --vram 256 --chr /path/to/tiles.chr --buttons 2

# Run with audio (always on when --vram is set; optionally override sample rate and buffer size)
dotnet run --project Emulator/Emulator.csproj -- --vram 256 --chr /path/to/tiles.chr --sample-rate 44100 --buffer-size 1024

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
                                                           CPU (execute) ←→ MMIO bus decoder ←→ PPU (render, VRAM)
                                                                        ↓
                                                                  Emulator (hosts CPU + PPU, debugger, JSON over stdin/stdout)
                                                                        ↓
                                                                  Neovim plugin (IDE)
```

### C# Projects (in `cpu-simple.sln`)

- **CPU/** - Core CPU library: fetch-decode-execute cycle, memory, stack, registers, flags (Zero/Carry). The `OpcodeFactory` uses reflection to discover opcode classes annotated with `[Opcode]` attribute implementing `IOpcode`. Current implementation treats all instructions as single-cycle, but a realistic timing model is documented in `docs/projects/cpu-simple/micro-code.md` (Fetch cost = instruction bytes, Decode = 0 cycles [parallel with fetch], Execute = 0/1/2 cycles based on operation type).
- **Assembler/** - CLI tool with four pipeline stages: `Lexer` (tokenizes) → `Parser` (builds AST) → `Analyser` (two-pass: builds symbol table, resolves labels) → `Emitter` (produces bytes). Supports `.text` and `.data` sections.
- **Emulator/** - Emulator and debugger hosting the CPU. Reads JSON commands from stdin, writes JSON responses to stdout. Commands are discovered via `[Command]` attribute, split into `GlobalCommands/` (dump, breakpoint, watchpoint, status) and `StateCommands/` (load, run, step, tick, stepover, stepout, reset). When `--vram SIZE` is passed, opens a Raylib window at ~60fps (`RunWindowed()`); otherwise runs headless at 10Hz (`RunHeadless()`). `--scale N` sets the pixel scale factor (default 4). `--chr PATH` loads CHR ROM tile data from a file. `--buttons N` enables the controller with N custom buttons (requires `--vram`). `--sample-rate HZ` and `--buffer-size N` configure audio output (always enabled with `--vram`; defaults 44100/1024; both require `--vram`). All flags can be set persistently in `emulator.json` in the working directory; CLI flags override any value set in the file. Peripherals (PPU, Controller, AudioChip) are grouped into `PeripheralSet` (`EmulatorApplication.cs`) and passed together to `CpuHandler`. Per-tick `status` JSON is suppressed in windowed mode to avoid flooding stdout. The `Display` class (`Emulator/Display.cs`) wraps the Raylib window and audio device lifecycle and is never instantiated in tests.
- **LanguageServer/** - LSP server for `.csasm` files (diagnostics, hover, completion). Uses the OmniSharp LSP SDK (`OmniSharp.Extensions.LanguageServer`) over stdio. Reuses the Assembler pipeline directly — runs Lexer→Parser→Analyser on each document change and translates exceptions into LSP diagnostics.
- **PPU/** - Picture Processing Unit with dedicated VRAM. Implements `IMmioDevice` via `PpuRegisters` (PPUADDR/PPUDATA/PPUSTATUS). Communicates with the CPU exclusively through MMIO-mapped registers; the CPU never addresses VRAM directly. Ticked alongside the CPU by the Emulator in a single-threaded co-simulation loop. Fires VBlank via `Ppu.VBlankStarted` (Emulator wires to `cpu.RequestInterrupt()`) then `Ppu.FrameReady` with RGB pixel data (`ScreenWidth × ScreenHeight × 3` bytes; Emulator wires to `display.UpdateFrame(rgb)`). The RGB conversion (monochrome 1bpp: `0→black`, non-zero→white) lives in `Ppu.ConvertFramebufferToRgb()`. Enabled by `--vram SIZE` emulator arg (passes `PpuConfig.Minimal8Bit` to `CpuHandler`; the PPU is created when `ppuConfig != null`). CHR tile data is passed via `--chr PATH` or defaults to a zero-filled buffer. `Ppu(PpuConfig, IReadOnlyList<byte>? chrData = null)` constructs `ChrRom` internally. Uses **raylib-cs** (C# bindings for raylib) for rendering output. See `docs/projects/cpu-simple/PPU.md` for the authoritative design reference.
- **CPU.Tests/**, **Assembler.Tests/**, **Emulator.Tests/**, and **LanguageServer.Tests/** - NUnit 4 test suites. Emulator.Tests uses `InternalsVisibleTo` to access internal Emulator types.

### Non-C# Components

- **nvim-plugin/** - Lua-based Neovim plugin providing `:Cpu*` commands and LSP auto-attach for `.csasm` files.
- **tree-sitter-grammar/** - Tree-sitter grammar for `.csasm` assembly syntax highlighting.

## Key Patterns

- **Opcode timing model**: `docs/projects/cpu-simple/micro-code.md` is the **authoritative reference** for opcode phase design. Every opcode must follow the tick rules defined there — each tick is either a single bus transaction (memory read/write, stack push/pop) OR a single internal operation (ALU, flag update, PC/EA update), never both. Bookkeeping (`PC++`, `SP±`) is allowed alongside a bus tick. Effective address calculations (indexed addressing) require their own internal tick. Refer to the doc when deciding how many phases an opcode needs and what `MicroPhase` value each should return.

- **8-bit vs 16-bit builds**: Controlled by `#if x16` conditional compilation. The `DebugX16`/`ReleaseX16` configurations define the `x16` symbol, which changes address sizes from 1 byte to 2 bytes throughout CPU and Assembler.
- **Attribute-based discovery**: Both opcodes (`[Opcode]` attribute + `IOpcode` interface) and emulator commands (`[Command]` attribute + `ICommand` interface) use reflection-based registries to auto-discover implementations.
- **Opcode constructor signature**: All opcodes must have constructor `(byte, State, IBus, Stack)`. `OpcodeFactory` discovers them via reflection using this exact parameter list.
- **Assembler debug output**: The assembler can emit a JSON debug file containing symbol table and span-to-address mappings for IDE integration.
- **Assembler exception patterns**: `Lexer` throws `LexerException` directly. `Parser.ParseProgram()` and `Analyser.Run()` collect errors and throw `AggregateException` wrapping `ParserException`/`AnalyserException` respectively. However, `Analyser.Run()` can also throw a bare `ParserException` from its `ResolveLabels()` phase (for unresolved label references). All assembler exceptions bake ` at line N, column M` into the message string and expose `Line`/`Column` properties (0-based).
- **Assembly syntax**: Memory address operands require square brackets (e.g., `jmp [label]`, `lda r0, [label]`, `ldx r1, [r0 + #0x01]`). Immediate values use `#` prefix (e.g., `ldi r0, #0x05`). The lexer lowercases all input and trims leading whitespace from each line before tokenizing but the original line number and column are preserved.
- **LSP SDK (OmniSharp)**: Uses `TextDocumentSelector` (not `DocumentSelector`) for handler registration. `CompletionHandlerBase` requires implementing both `Handle(CompletionParams, ...)` and `Handle(CompletionItem, ...)` (resolve). Markup content uses `MarkupKind.Markdown`.
- **CPU inspector from state commands**: Emulator state commands can inspect CPU state via `CpuStateFactory.GetInspector()`. The `stepover` and `stepout` commands use this to read memory/stack and then delegate to `RunningState(ToAddress)` for breakpoint-aware execution.
- **Tick vs Step**: `CPU.Step()` runs micro-ticks in a loop until `IsInstructionComplete`, accumulating one `TickTrace` per micro-tick. `CPU.Tick()` advances exactly one micro-tick and records one trace. `SteppingState` uses `Step()`, `TickingState` uses `Tick()`. Both states produce identical `status` JSON — only the number of `traces` entries differs.
- **Watchpoints**: `WatchpointContainer` (in `Emulator/WatchpointContainer.cs`) holds `IWatchpoint` instances. Two concrete types: `AddressWatchpoint` (matches `Bus.Type==Memory` + direction + address on a `TickTrace`) and `PhaseWatchpoint` (matches `NextPhase` on a `TickTrace`). `ExecutingCpuState.Tick()` calls `watchpointContainer.Check(inspector.Traces)` after the breakpoint check — a match transitions to `IdleState` and emits `watchpoint_hit` JSON. `WatchpointContainer` and `IWatchpoint` are threaded through `CpuStateFactory` and `GlobalCommandExecutionContext`. The `watchpoint` global command (alias `wp`) manages watchpoints; `WatchpointContainer.NextId()` vends auto-incrementing IDs that never reset.
- **Plugin source buffer targeting**: The plugin's `highlight_pc()` and `highlight_breakpoints()` functions use `assembler.assembler.last_source_bufnr` to target the correct source buffer instead of relying on `nvim_get_current_buf()`, which can return a sidebar panel buffer.
- **PPU design**: `docs/projects/cpu-simple/PPU.md` is the **authoritative reference** for all PPU architecture decisions (MMIO layout, VRAM sizing, PPUADDR write-latch, tile/sprite model, scanline pipeline, co-simulation, debugger integration). Key constraints: PPU is never on the same OS thread as the CPU — use single-threaded co-simulation only; PPU watchpoints and PPU tick traces are separate subsystems from CPU watchpoints and `TickTrace`; MMIO register count is severely constrained in 8-bit mode (256-byte address space shared with code and RAM).
- **Windowed vs headless mode**: `EmulatorApplication.Run()` dispatches to `RunWindowed()` when `context.Peripherals != null` (i.e., `--vram` was passed), otherwise `RunHeadless()`. Windowed mode implies Raylib keyboard input polling: each iteration of `RunWindowed` calls `_display.PollInput(buttonState)` before `TickFrame()` when a controller is configured (`_cpuHandler.ButtonState != null`). `CpuHandler.ButtonState` (`Controller.Storage.ButtonsState?`) is the bridge — it returns `_controller?.ButtonState` and is null when no controller is present. All Raylib key reads live exclusively in `Display.PollInput` (arrow keys → directions; Z/X/A/S → buttons 0–3; `IsKeyDown` so held keys keep the latch set each frame). Because `Display` is `internal` and never instantiated in tests, `Display.PollInput` is not unit-testable; `CpuHandler.ButtonState` is tested instead. Controller requires PPU (enforced by `ValidateArgs`: `--buttons` without `--vram` is rejected). `PeripheralSet` makes this structural: its `PpuConfig` is non-nullable, so a Controller-only `PeripheralSet` is unrepresentable. Audio is always enabled when PPU is present; `PeripheralSet.AudioConfig` (`AudioConfiguration?`) carries sample rate and buffer size.
- **Audio chip wiring**: `AudioChip` is created in `CpuHandler` when `PeripheralSet.AudioConfig != null` and registered in the `MmioRouter` at offset `0x04` (size 4, physical addresses `0xEC–0xEF` in 8-bit mode). `CpuHandler.AudioChip` exposes it as a nullable property. Audio is ticked in a batch at the end of `TickFrame()`: `TotalScanlines × CyclesPerScanline / PpuCyclesPerCpuCycle` calls to `AudioChip.Tick()`. `CpuClockRate` for `AudioConfiguration` is derived from `PpuConfig` at startup — not user-configurable. In `RunWindowed()`, `EmulatorApplication` submits samples to `Display.SubmitAudio()` each frame when `IsAudioStreamProcessed()` returns true and `AudioChip.AvailableSamples >= bufferSize`. Raylib audio stream lifecycle (init, load, play, stop, unload, close device) lives entirely in `Display`. See `docs/projects/cpu-simple/audio.md` for the authoritative APU design reference.
- **`CpuHandler.TickFrame()`**: Runs `(TotalScanlines × CyclesPerScanline) / PpuCyclesPerCpuCycle` state ticks (budget for one full frame). Stops early if the CPU transitions to idle/halted/error, then ticks the remaining PPU cycles so `FrameReady` still fires at end-of-frame. PPU ticks per state tick = `microTicks × PpuCyclesPerCpuCycle`, where `microTicks` is the CPU trace count for executing states (approximates CPU micro-tick count) or 1 otherwise. After PPU completes, audio is batched: `AudioChip.Tick()` is called `TotalScanlines × CyclesPerScanline / PpuCyclesPerCpuCycle` times in one burst to generate the frame's worth of samples into the ring buffer.
- **MMIO bus decoder**: `BusDecoder` (in `CPU/components/BusDecoder.cs`) implements `IBus` and sits between the CPU and all addressable devices. Routes addresses `0x00–0xE7` to `Memory` and `0xE8–0xEF` to the `IMmioDevice` in 8-bit mode; `0x0000–0xEEFF` to RAM and `0xEF00–0xEFFF` to MMIO in 16-bit mode. `IMmioDevice.ReadRegister`/`WriteRegister` receive byte offsets (base address already subtracted). The Emulator assembles MMIO devices in `CpuHandler` from `PeripheralSet`: PPU is always registered at offset `0x00` (size 3) when peripherals are present; Controller is optionally registered at offset `0x03` (size 1); AudioChip is optionally registered at offset `0x04` (size 4). When no peripherals are present, no MMIO router is created. All bus accesses are recorded as `BusType.Memory` so existing `AddressWatchpoint` works on MMIO register addresses. The stack is unaffected — stack operations bypass the address bus entirely.

## Test Framework

NUnit 4 with `Microsoft.NET.Test.SDK`. Tests use `[Test]` and `[TestCase]` attributes.

### Test Helpers
- **Assembler tests**: `AnalyserTestsHelper.AnalyseProgram()` runs Lex→Parse→Analyse and returns emit nodes. `AnalyseAndEmit()` goes through the full pipeline to bytes. `GetSymbols()` returns the symbol table.
- **Emulator tests**: `EmulatorTestHelpers.CreateGlobalContext()` creates a `GlobalCommandExecutionContext` with a real CPU and test doubles (`TestLogger`, `TestOutput`). Emulator's `ParseArgs` and `ValidateArgs` are `internal` for direct testability.
