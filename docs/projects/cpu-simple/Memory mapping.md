# Memory mapping

## On the relation between Memory, Stack and the mapper

**Should the mapper also include the stack and expose push/pop on top of read/write?**

This is a legitimate architectural instinct - wanting a single entry point for all memory operations. Let me break down what you're proposing and where it leads.

---

### What you're actually describing

Your proposal is essentially: **make the stack fully memory-mapped**.

On real 8-bit hardware (6502, Z80), this is exactly how it works:
- The stack is just a region of main RAM (e.g., addresses `0x0100-0x01FF` on the 6502)
- `push` = decrement SP, then `WriteByte(SP, value)` - a normal memory write
- `pop` = `ReadByte(SP)`, then increment SP - a normal memory read
- SP is just a register pointing into that region
- There is no separate "stack object" - the bus routes to RAM, which happens to be used as a stack

That model gives you the single entry point you want. The memory mapper routes `[0x00-0xCF]` to RAM, `[0xD0-0xDF]` to the stack region, `[0xF0-0xFF]` to PPU registers.

---

### The problem with your specific proposal

Exposing `push`/`pop` **from the memory mapper itself** mixes abstraction levels:

- The mapper's job is **address routing** - given an address, send the access to the right component. It knows nothing about stack discipline.
- `push`/`pop` are **opcode-level semantics** - they encode SP management, not just address translation.

If the mapper exposes `push`/`pop`, it now needs to know what SP is, which direction the stack grows, etc. That's state management, not routing.

The two clean options are:

| Option | Model | Stack access |
|---|---|---|
| **A** | Stack fully memory-mapped (real hardware) | Opcodes call `mapper.Write(sp--, value)` |
| **B** | Stack as separate component (current) | Opcodes call `stack.Push(value)` directly |

Your proposal is a hybrid that doesn't fully commit to either, and likely ends up with more coupling, not less.

---

### The scope problem

Option A is architecturally cleaner and more authentic, but it means:
- Changing the `Stack` class to be backed by an address region
- Changing all stack opcodes (`PSH`, `POP`, `CAL`, `RET`, `RTI`...)
- Changing the opcode constructor signature `(State, Memory, Stack, OpcodeArgs)` ΓåÆ `(State, MemoryMapper, OpcodeArgs)` across 60+ files
- Re-verifying all stack-related tests

Per the project's own principles: **don't refactor adjacent code during a PPU feature**. This is valid to address, but as a separate, deliberate architectural PR - not bundled into PPU work.

---

**My recommendation:** File the observation, do it intentionally later. For the PPU work, the memory mapper only needs to intercept the MMIO address range. The stack can stay as-is.