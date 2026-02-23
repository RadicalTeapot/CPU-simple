# Debugging

This document describes how debugging works end-to-end in CPU-simple: from the tick-level trace capture inside the CPU, through the Backend JSON protocol, to the Neovim IDE.

## Architecture Overview

```
CPU (TickHandler)
   │  captures TickTrace per tick
   ▼
CpuInspector.Traces
   │  exposed by CPU.GetInspector()
   ▼
Backend (ConsoleOutput)
   │  serialises as JSON "status" → stdout
   ▼
nvim-plugin (state.lua)
   │  parses traces, builds memory_changes / stack_changes
   ▼
display/memory.lua, display/registers.lua, …
   │  highlights changed cells, shows register values
   ▼
Neovim buffer (sidebar + source file)
```

---

## 1. Trace Capture (CPU layer)

### TickHandler

Every call to `TickHandler.Tick()` performs one micro-step and produces a `TickTrace`. The handler:

1. **Clears** the `BusRecorder`.
2. **Snapshots** PC, SP, registers, Z, C before running any logic.
3. **Records the executed phase** (`executedPhase = _currentPhase`) before advancing the state machine.
4. **Runs existing tick logic** unchanged — fetch or opcode `Tick()` call.
5. **Diffs** registers to find changes.
6. **Reads** `BusRecorder.LastAccess` for the bus transaction (if any).
7. **Classifies** the executed phase into `TickType` (BusRead / BusWrite / Internal).
8. **Constructs** and returns a `TickTrace` embedded in `MicrocodeTickResult`.

### BusRecorder

A shared `BusRecorder` instance is wired to both `Memory.Recorder` and `Stack.Recorder` in the `TickHandler` constructor. When an opcode calls `ReadByte` or `WriteByte` on either object, the recorder silently captures the address, data, and direction without any opcode knowing about it.

**What gets recorded:**
- Typed memory reads/writes: `Memory.ReadByte(byte/ushort)`, `Memory.WriteByte(byte/ushort, …)`
- Stack push/pop: recorded at the `Stack` level (not via `Stack._memory`, to avoid double-recording)
- Opcode fetch: `Memory.ReadByte` used by `TickHandler.FetchCurrentInstruction` is typed (byte/ushort), so it is also recorded

**What is not recorded:**
- Debug overloads: `Memory.ReadByte(int)` — used only by the Backend debugger for inspecting state
- Bulk operations: `Memory.ReadBytes`, `Memory.LoadBytes` — not bus transactions

### Data model

```csharp
// CPU/microcode/TickTrace.cs
public enum TickType { BusRead, BusWrite, Internal }
public enum BusDirection { Read, Write }
public record BusAccess(int Address, byte Data, BusDirection Direction);
public record RegisterChange(int Index, byte OldValue, byte NewValue);
public record TickTrace(
    ulong TickNumber,
    TickType Type,
    MicroPhase Phase,
    int PcBefore, int PcAfter,
    int SpBefore, int SpAfter,
    string Instruction,
    RegisterChange[] RegisterChanges,
    bool ZeroFlagBefore, bool ZeroFlagAfter,
    bool CarryFlagBefore, bool CarryFlagAfter,
    BusAccess? Bus           // null on internal ticks
);
```

---

## 2. Trace Exposure (CPU public API)

`CPU.Step()` and `CPU.Tick()` both clear `_lastTraces` before running, then collect traces from each `MicrocodeTickResult`.

- `CPU.Step()` accumulates one trace per micro-tick across the full instruction.
- `CPU.Tick()` accumulates a single trace for the one micro-tick it executes.

`CPU.GetInspector()` passes `[.. _lastTraces]` to `CpuInspector.Create(...)`, which stores them in `CpuInspector.Traces`. `Reset()` clears `_lastTraces`.

The Backend accesses traces only via `GetInspector()` — it never touches `TickHandler` directly.

---

## 3. Backend Serialisation

`ConsoleOutput.WriteStatus` in `Backend/IO/IOutput.cs` serialises each `TickTrace` into a JSON object within the `traces` array of the `status` message.

Relevant JSON fields per trace:

| JSON field | Source |
|---|---|
| `tick` | `TickTrace.TickNumber` |
| `tick_type` | `TickTrace.Type.ToString()` — `"BusRead"`, `"BusWrite"`, `"Internal"` |
| `phase` | `TickTrace.Phase.ToString()` — matches `MicroPhase` enum name |
| `pc_before` / `pc_after` | PC before/after the tick |
| `sp_before` / `sp_after` | SP before/after the tick |
| `instruction` | Opcode name (e.g. `"LDA"`, `"ADD"`) |
| `register_changes` | Array of `{ index, old_value, new_value }` |
| `zero_flag_before` / `zero_flag_after` | Zero flag state |
| `carry_flag_before` / `carry_flag_after` | Carry flag state |
| `bus` | `{ address, data, direction }` or `null` |

The `status` command handler (`Backend/Commands/GlobalCommands/Status.cs`) shows a compact human-readable summary: `[T{tick} {phase} {type}]` per trace.

---

## 4. Plugin State Management

`nvim-plugin/lua/cpu-simple/state.lua` is the single source of truth for CPU state in the plugin.

### Parsing traces

`M.update_status(json)` iterates `json.traces` to derive memory and stack change maps:

```lua
-- Bus writes where SP changed = stack write
-- Bus writes where SP unchanged = main memory write
for _, trace in ipairs(json.traces) do
    if trace.bus and trace.bus.direction == "Write" then
        if trace.phase == "MemoryWrite" then
            if trace.sp_before ~= trace.sp_after then
                stack_changes[trace.bus.address] = trace.bus.data
            else
                memory_changes[trace.bus.address] = trace.bus.data
            end
        end
    end
end
```

These maps are stored in `M.status.memory_changes` and `M.status.stack_changes` (0-based address keys, byte values), and also applied incrementally to `M.memory` and `M.stack` arrays (1-based Lua indices) so the display panels stay current without a full dump after every step.

### State table shape

```lua
M.status = {
    cycles       = number,
    pc           = number,
    sp           = number,
    registers    = { [0]=byte, [1]=byte, … },   -- 0-based
    flags        = { zero = bool, carry = bool },
    memory_changes = { [addr]=byte, … },         -- 0-based addresses
    stack_changes  = { [addr]=byte, … },         -- 0-based addresses
    loaded_program = bool,
}
M.memory = { byte, byte, … }  -- 1-based, full memory image
M.stack  = { byte, byte, … }  -- 1-based, full stack image
```

---

## 5. Stepping vs Ticking

The Backend exposes two granularities:

| Backend command | CPU call | Traces produced |
|---|---|---|
| `step` | `CPU.Step()` | One trace per micro-tick across the full instruction |
| `tick` (if exposed) | `CPU.Tick()` | One trace for the single micro-tick executed |

From the plugin's perspective both produce the same `status` JSON; only the number of entries in `traces` differs.

---

## 6. Worked Example: `LDA r0, [0x0C]` (8-bit mode)

Instruction bytes: `[0x14, 0x0C]` — 3 ticks total.

After `CPU.Step()`, `CpuInspector.Traces` contains:

```
Trace[0]: TickNumber=N  Phase=FetchOpcode  Type=BusRead
          PcBefore=0  PcAfter=1
          Bus={ Address=0, Data=0x14, Direction=Read }
          RegisterChanges=[]

Trace[1]: TickNumber=N+1  Phase=FetchOperand  Type=BusRead
          PcBefore=1  PcAfter=2
          Bus={ Address=1, Data=0x0C, Direction=Read }
          RegisterChanges=[]

Trace[2]: TickNumber=N+2  Phase=MemoryRead  Type=BusRead
          PcBefore=2  PcAfter=2
          Bus={ Address=0x0C, Data=<mem[0x0C]>, Direction=Read }
          RegisterChanges=[{ Index=0, OldValue=?, NewValue=<mem[0x0C]> }]
```

The plugin receives all three traces in the `status.traces` array. Since none are `MemoryWrite` bus events, `memory_changes` and `stack_changes` remain empty — only the register display updates.

---

## 7. Worked Example: `STA r0, [0x10]` (8-bit mode)

Instruction bytes: `[0x24, 0x10]` — 3 ticks. Assuming `r0 = 0x42`.

```
Trace[0]: Phase=FetchOpcode   Type=BusRead   Bus={0, 0x24, Read}
Trace[1]: Phase=FetchOperand  Type=BusRead   Bus={1, 0x10, Read}
Trace[2]: Phase=MemoryWrite   Type=BusWrite  Bus={0x10, 0x42, Write}
          SpBefore=SpAfter (no SP change → main memory write)
```

The plugin classifies Trace[2] as a memory write (SP unchanged), so `memory_changes[0x10] = 0x42`. The memory display panel highlights address `0x10`.

---

## 8. Worked Example: `PSH r0` (push to stack)

Instruction bytes: `[0x20]` — 2 ticks. Assuming SP starts at 15, `r0 = 0x07`.

```
Trace[0]: Phase=FetchOpcode  Type=BusRead   Bus={0, 0x20, Read}
          SpBefore=15  SpAfter=15

Trace[1]: Phase=MemoryWrite  Type=BusWrite  Bus={15, 0x07, Write}
          SpBefore=15  SpAfter=14           ← SP changed
```

The plugin sees `sp_before != sp_after` on Trace[1], so classifies it as a stack write: `stack_changes[15] = 0x07`. The stack display panel highlights that address.
