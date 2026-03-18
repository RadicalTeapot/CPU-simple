# Adding New Instructions to the ISA

This guide covers every layer you need to touch when adding a new instruction to CPU-simple: opcode implementation, assembler, language server, and tree-sitter grammar.

## Overview

A new instruction propagates through these layers in order:

```
CPU/opcodes/       — implement the opcode behaviour
CPU/opcodes/IOpcode.cs — register the opcode code
Assembler/Analyser.cs  — teach the assembler to emit it
LanguageServer/        — add hover docs and completion
tree-sitter-grammar/   — update syntax highlighting (if it's a new directive)
Tests                  — cover the new behaviour
```

---

## Step 1 — Add the opcode byte value

**File: `CPU/opcodes/IOpcode.cs`** (`OpcodeBaseCode` enum)

Pick an unused value in the `SystemAndJump` group (`0x00`–`0x0F`) or another group if the instruction takes register operands. The group determines what mask the opcode factory uses to decode register indices.

```csharp
// Before
SEZ = 0x05,
JMP = 0x08,

// After — adding SEI (0x06) and CLI (0x07)
SEZ = 0x05,
SEI = 0x06,
CLI = 0x07,
JMP = 0x08,
```

> **Encoding rule**: Instructions with no operands live in `SystemAndJump` (mask `0xFF`). Instructions with one register use groups with mask `0xFC` (bits 1–0 encode the register). Two-register instructions use mask `0xF0` (bits 3–0 encode both registers). See `OpcodeGroup.cs` for all group/mask definitions.

---

## Step 2 — Implement the opcode

Create a file in `CPU/opcodes/`. Follow one of two patterns:

### Zero-execute-tick instructions (flag ops, NOP, MOV, …)

Implement `IOpcode` directly — no base class needed. Return `MicroPhase.Done` from `GetStartPhaseType()` so the TickHandler knows there are no execute phases.

```csharp
// Template: SEZ.cs, CLZ.cs, CLC.cs
[Opcode(OpcodeBaseCode.SEI, OpcodeGroupBaseCode.SystemAndJump)]
internal class SEI(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
{
    public MicroPhase GetStartPhaseType() => MicroPhase.Done;

    public MicroPhase Tick(uint phaseCount)
    {
        state.SetInterruptDisableFlag(true);
        return MicroPhase.Done;
    }
}
```

### Multi-tick instructions (memory reads/writes, stack ops, …)

Extend `BaseOpcode` and call `SetPhases()` in the constructor to declare the execute-phase sequence. Each delegate returns the `MicroPhase` type of the next tick (or `Done` when finished).

```csharp
// Template: RET.cs, CAL.cs
[Opcode(OpcodeBaseCode.RTI, OpcodeGroupBaseCode.SystemAndJump)]
internal class RTI : BaseOpcode
{
    public RTI(byte instructionByte, State state, Memory memory, Stack stack)
    {
        _state = state;
        _stack = stack;
#if x16
        SetPhases(MicroPhase.MemoryRead, PopPCLow, PopPCHigh, ComposePC, PopStatus);
#else
        SetPhases(MicroPhase.MemoryRead, PopPC, PopStatus);
#endif
    }

    private MicroPhase PopPC()
    {
        _state.SetPC(_stack.PopByte());
        return MicroPhase.MemoryRead;
    }

    private MicroPhase PopStatus()
    {
        var s = _stack.PopByte();
        _state.SetZeroFlag((s & 0x01) != 0);
        _state.SetCarryFlag((s & 0x02) != 0);
        return MicroPhase.Done;
    }

    private readonly State _state;
    private readonly Stack _stack;
}
```

**Constructor signature rule**: All opcodes must have constructor `(byte instructionByte, State state, Memory memory, Stack stack)`. The `[Opcode]` attribute and this signature are what `OpcodeFactory` uses for reflection-based auto-discovery — no manual registration needed.

**Tick rules**: Consult `docs/projects/cpu-simple/micro-code.md` before deciding how many phases your instruction needs. The key rule is: each tick is **either** a bus transaction (memory read/write) **or** an internal operation (ALU, composition) — never both. Simple bookkeeping like `PC++` or `SP±` is free and can accompany a bus tick.

**8-bit vs 16-bit**: Use `#if x16` / `#else` blocks wherever the instruction behaves differently due to address width. See `RET.cs` and `CAL.cs` for examples.

---

## Step 3 — Wire the assembler

**File: `Assembler/Analyser.cs`** — `HandleInstruction()` method.

Add a `case` for the new opcode and choose the appropriate analysis node:

| Node type | Operand pattern | Examples |
|-----------|----------------|---------|
| `NoOperandNode` | none | `NOP`, `RET`, `SEI`, `RTI` |
| `SingleMemoryAddressNode` | `[label]` | `JMP`, `CAL` |
| `SingleRegisterNode` | `rN` | `PSH`, `POP`, `INC` |
| `RegisterAndImmediateNode` | `rN, #val` | `LDI`, `ADI` |
| `RegisterAndMemoryAddressNode` | `rN, [addr]` | `LDA`, `STA` |
| `TwoRegisterNode` | `rDst, rSrc` | `MOV`, `ADD` |

```csharp
case OpcodeBaseCode.SEI:
case OpcodeBaseCode.CLI:
case OpcodeBaseCode.RTI:
    CurrentSection.Nodes.Add(new NoOperandNode(instruction, opcode));
    break;
```

If the new instruction should only be allowed in certain sections (e.g., only in `.text` and `.irq`, not `.data`), the existing section-type check in `HandleStatement()` already enforces this — data sections reject instructions, text and IRQ sections reject data directives.

---

## Step 4 — Update the language server

### Hover documentation

**File: `LanguageServer/InstructionDescriptions.cs`**

Add an entry to the `Entries` dictionary. The key is the lowercase mnemonic; the value is `(description, syntax)`.

```csharp
["sei"] = ("Set interrupt disable flag. Prevents interrupts from being serviced.", "sei"),
["cli"] = ("Clear interrupt disable flag. Allows interrupts to be serviced.", "cli"),
["rti"] = ("Return from interrupt. Restores PC and flags (Z, C, I) from stack.", "rti"),
```

Hover is automatically available — no handler changes needed. The `HoverHandler` looks up the mnemonic in this dictionary when the cursor is on an instruction.

### Completion operand type

**File: `LanguageServer/Handlers/CompletionHandler.cs`**

Add the mnemonic to the appropriate `HashSet` that matches its operand pattern. This controls what the completion handler suggests after the mnemonic:

```csharp
private static readonly HashSet<string> NoOperandOpcodes =
    ["nop", "hlt", "clc", "sec", "clz", "sez", "sei", "cli", "ret", "rti"];
```

Mnemonic completions (when the cursor is at the start of a line) are generated automatically from `OpcodeBaseCode` enum values via reflection — no change needed there.

---

## Step 5 — Update tree-sitter (directives only)

The tree-sitter grammar does **not** need changes for new instructions — mnemonics are parsed as generic `identifier` nodes, so any new mnemonic works automatically.

You **do** need a grammar change if you're adding a new **section directive** (like `.irq`).

**File: `tree-sitter-grammar/grammar.js`** — `header_directive` rule:

```javascript
// Before
header_directive: ($) =>
  seq(".", choice(/[tT][eE][xX][tT]/, /[dD][aA][tT][aA]/)),

// After — adding .irq
header_directive: ($) =>
  seq(".", choice(/[tT][eE][xX][tT]/, /[dD][aA][tT][aA]/, /[iI][rR][qQ]/)),
```

After editing `grammar.js`, **always regenerate the parser**:

```bash
cd tree-sitter-grammar
npx tree-sitter generate
npm test
```

The `tree-sitter generate` step compiles the grammar into the C parser that tree-sitter uses. Without regeneration the grammar change has no effect. Add a corpus test in `test/corpus/directives.txt` to lock in the expected parse tree:

```
================================================================================
IRQ section directive
================================================================================
.irq
--------------------------------------------------------------------------------

(source_file
  (line
    (statement
      (header_directive))))
```

If you're adding a new **data directive** (like `.byte`), update `directive_name` instead:

```javascript
directive_name: ($) =>
  seq(
    ".",
    choice(
      /[bB][yY][tT][eE]/,
      /[nN][eE][wW][dD][iI][rR]/,   // ← new
      // ...
    )
  ),
```

---

## Step 6 — Tests

### CPU tests

Follow the pattern in `CPU.Tests/Opcodes_tests.cs`. Each instruction gets a `[TestFixture]` class:

```csharp
[TestFixture]
public class SEI_tests
{
    [Test]
    public void SEI_SetsInterruptDisableFlag()
    {
        var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
            program: [(byte)OpcodeBaseCode.SEI],
            out var state, out _, out _);
        state.SetInterruptDisableFlag(false);

        cpu.Step();

        Assert.That(state.I, Is.True);
        Assert.That(state.GetPC(), Is.EqualTo(1));
    }
}
```

For multi-tick instructions, also add a tick-sequence test in `Opcodes_tick_tests.cs` to lock in the phase sequence:

```csharp
[Test]
public void Rti()
{
    var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
        program: [(byte)OpcodeBaseCode.RTI],
        out _, out var stack, out _);
    stack.PushByte(0x00);   // status byte
    stack.PushAddress(0x00);

#if x16
    MicroPhase[] expected = [MemoryRead, MemoryRead, ValueComposition, MemoryRead, FetchOpcode];
#else
    MicroPhase[] expected = [MemoryRead, MemoryRead, FetchOpcode];
#endif
    Assert.That(TickSequence(cpu), Is.EqualTo(expected));
}
```

Zero-execute-tick instructions can share the existing `ZeroExecute` parameterised test — just add a `[TestCase]`:

```csharp
[TestCase((byte)OpcodeBaseCode.SEI)]
[TestCase((byte)OpcodeBaseCode.CLI)]
```

### Assembler tests

Add cases in `Assembler.Tests/Analyser_tests.cs` that verify the opcode byte and any error conditions:

```csharp
[TestCase("SEI", (byte)OpcodeBaseCode.SEI)]
[TestCase("CLI", (byte)OpcodeBaseCode.CLI)]
[TestCase("RTI", (byte)OpcodeBaseCode.RTI)]
public void InterruptOpcodes_AssembleCorrectly(string mnemonic, byte expectedByte)
{
    var bytes = AnalyserTestsHelper.AnalyseAndEmit(mnemonic);
    Assert.That(bytes[0], Is.EqualTo(expectedByte));
}
```

---

## Adding a new section directive (e.g., `.irq`)

Section directives require more assembler work than instructions. The full checklist:

1. **`Assembler/Analysis/Section.cs`** — add the new type to the `Section.Type` enum
2. **`Assembler/Analyser.cs`** — handle the directive keyword in `HandleHeaderDirective()`, enforce any constraints (duplicate check, allowed content types)
3. **`Assembler/Analyser.cs`** — update `Run()` if the section has non-sequential placement (fixed address, fill gap)
4. **`LanguageServer/DirectiveDescriptions.cs`** — add description entry
5. **`LanguageServer/Handlers/CompletionHandler.cs`** — add to `DirectiveNames` array and `GetSectionDirectiveCompletions()`
6. **Tree-sitter** — update `header_directive` rule, regenerate, add corpus test (see Step 5 above)

---

## Checklist

- [ ] `OpcodeBaseCode` enum value added
- [ ] Opcode class created with `[Opcode]` attribute and correct constructor signature
- [ ] `Analyser.HandleInstruction()` case added (correct node type)
- [ ] `InstructionDescriptions.Entries` entry added
- [ ] Mnemonic added to correct `*Opcodes` HashSet in `CompletionHandler`
- [ ] Tree-sitter grammar updated and parser regenerated (directive only)
- [ ] `micro-code.md` updated with timing breakdown
- [ ] CPU unit tests added (`Opcodes_tests.cs`, `Opcodes_tick_tests.cs`)
- [ ] Assembler tests added (`Analyser_tests.cs`)
- [ ] `dotnet build cpu-simple.sln -c Debug` passes
- [ ] `dotnet build cpu-simple.sln -c DebugX16` passes
- [ ] `dotnet test cpu-simple.sln -c Debug` passes
- [ ] `npm test` (tree-sitter) passes (if grammar changed)
