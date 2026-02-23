# Debugging

This document describes how debugging works end-to-end in CPU-simple: from the tick-level trace capture inside the CPU, through the Backend JSON protocol, to the Neovim IDE.

## Architecture Overview

```
CPU (TickTracer)
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

### TickTracer

`TickTracer` owns all trace-capture responsibility. `CPU` holds a `TickTracer` instance and calls it around every `TickHandler.Tick()` call:

1. **`Prepare()`** — called *before* each `TickHandler.Tick()`:
   - Clears the `BusRecorder`.
   - Snapshots PC, SP, registers, Z, C.

2. **`TickHandler.Tick()`** — executes one micro-step and returns a `MicrocodeTickResult` containing `ExecutedPhase` (the phase that was run) and `NextPhase` (the next phase to run).

3. **`Record(MicrocodeTickResult)`** — called *after* each tick:
   - Reads after-state (PC, SP, flags, registers) from the CPU components.
   - Diffs registers against the snapshot to find changes.
   - Reads `BusRecorder.LastAccess` for the bus transaction (if any).
   - Classifies `ExecutedPhase` into `TickType` (`Bus` or `Internal`).
   - Constructs a `TickTrace` and appends it to the internal list.

### BusRecorder

A shared `BusRecorder` instance is wired to both `Memory.Recorder` and `Stack.Recorder` in the `TickTracer` constructor. When an opcode calls `ReadByte` or `WriteByte` on either object, the recorder silently captures the address, data, and direction without any opcode knowing about it.

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
public enum TickType { Bus, Internal }
public enum BusDirection { Read, Write }
public record BusAccess(int Address, byte Data, BusDirection Direction, BusType Type);
public record RegisterChange(int Index, byte OldValue, byte NewValue);
public record TickTrace(
    ulong TickNumber,
    TickType Type,
    MicroPhase NextPhase,
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

`CPU.Step()` and `CPU.Tick()` both call `_tracer.Clear()` before running, then call `_tracer.Prepare()` / `TickHandler.Tick()` / `_tracer.Record()` for each tick.

- `CPU.Step()` loops until the instruction is complete, accumulating one trace per micro-tick.
- `CPU.Tick()` runs a single micro-tick and accumulates one trace.

`CPU.GetInspector()` passes `_tracer` directly to the `CpuInspector` constructor, which reads `tracer.LastTraces` into `CpuInspector.Traces`. `Reset()` calls `_tracer.Clear()`.

The Backend accesses traces only via `GetInspector()` — it never touches `TickHandler` or `TickTracer` directly.

---

## 3. Backend Serialisation

`ConsoleOutput.WriteStatus` in `Backend/IO/IOutput.cs` serialises each `TickTrace` into a JSON object within the `traces` array of the `status` message.

Relevant JSON fields per trace:

| JSON field | Source |
|---|---|
| `tick` | `TickTrace.TickNumber` |
| `tick_type` | `TickTrace.Type.ToString()` — `"Bus"` or `"Internal"` |
| `next_phase` | `TickTrace.NextPhase.ToString()` — the phase that will execute *next* (will be used when debugging in tick mode to break at before given phase types)|
| `pc_before` / `pc_after` | PC before/after the tick |
| `sp_before` / `sp_after` | SP before/after the tick |
| `instruction` | Opcode name (e.g. `"LDA"`, `"ADD"`) |
| `register_changes` | Array of `{ index, old_value, new_value }` |
| `zero_flag_before` / `zero_flag_after` | Zero flag state |
| `carry_flag_before` / `carry_flag_after` | Carry flag state |
| `bus` | `{ address, data, direction, type }` or `null` |

The `status` command handler (`Backend/Commands/GlobalCommands/Status.cs`) shows a compact human-readable summary: `[T{tick} {type} {bus-direction}]` per trace.

---

## 4. Plugin State Management

`nvim-plugin/lua/cpu-simple/state.lua` is the single source of truth for CPU state in the plugin.

### Parsing traces

`M.update_status(json)` iterates `json.traces` to derive memory and stack change maps:

```lua
-- Bus writes where bus.type == "Stack" = stack write
-- Bus writes where bus.type == "Memory" = main memory write
for _, trace in ipairs(json.traces) do
    if trace.tick_type == "Bus" and trace.bus and trace.bus.direction == "Write" then
        if trace.bus.type == "Stack" then
            stack_changes[trace.bus.address] = trace.bus.data
        else
            memory_changes[trace.bus.address] = trace.bus.data
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
Trace[0]: TickNumber=N  Type=Bus  NextPhase=FetchOperand
          PcBefore=0  PcAfter=1
          Bus={ Address=0, Data=0x14, Direction=Read, Type=Memory }
          RegisterChanges=[]

Trace[1]: TickNumber=N+1  Type=Bus  NextPhase=MemoryRead
          PcBefore=1  PcAfter=2
          Bus={ Address=1, Data=0x0C, Direction=Read, Type=Memory }
          RegisterChanges=[]

Trace[2]: TickNumber=N+2  Type=Bus  NextPhase=FetchOpcode
          PcBefore=2  PcAfter=2
          Bus={ Address=0x0C, Data=<mem[0x0C]>, Direction=Read, Type=Memory }
          RegisterChanges=[{ Index=0, OldValue=?, NewValue=<mem[0x0C]> }]
```

The plugin receives all three traces in the `status.traces` array. Since none have `bus.direction == "Write"`, `memory_changes` and `stack_changes` remain empty — only the register display updates.

---

## 7. Worked Example: `STA r0, [0x10]` (8-bit mode)

Instruction bytes: `[0x24, 0x10]` — 3 ticks. Assuming `r0 = 0x42`.

```
Trace[0]: Type=Bus  NextPhase=FetchOperand  Bus={0, 0x24, Read, Memory}
Trace[1]: Type=Bus  NextPhase=MemoryWrite   Bus={1, 0x10, Read, Memory}
Trace[2]: Type=Bus  NextPhase=FetchOpcode   Bus={0x10, 0x42, Write, Memory}
```

The plugin sees `bus.direction == "Write"` and `bus.type == "Memory"` on Trace[2], so `memory_changes[0x10] = 0x42`. The memory display panel highlights address `0x10`.

---

## 8. Worked Example: `PSH r0` (push to stack)

Instruction bytes: `[0x20]` — 2 ticks. Assuming SP starts at 15, `r0 = 0x07`.

```
Trace[0]: Type=Bus  NextPhase=MemoryWrite  Bus={0, 0x20, Read, Memory}
          SpBefore=15  SpAfter=15

Trace[1]: Type=Bus  NextPhase=FetchOpcode  Bus={15, 0x07, Write, Stack}
          SpBefore=15  SpAfter=14
```

The plugin sees `bus.direction == "Write"` and `bus.type == "Stack"` on Trace[1], so `stack_changes[15] = 0x07`. The stack display panel highlights that address.
