using CPU.opcodes;
namespace CPU.microcode
{
    internal record MicrocodeTickResult(
        ulong TickCount,
        MicroPhase CurrentPhase,
        int PhaseCount,
        OpcodeBaseCode CurrentOpcode,
        bool IsInstructionComplete = false
    ) { }
}
