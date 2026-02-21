using CPU.opcodes;
namespace CPU.microcode
{
    internal record MicrocodeTickResult(
        ulong TickCount,
        MicroPhase NextPhase,
        int PhaseCount,
        OpcodeBaseCode CurrentOpcode,
        bool IsInstructionComplete = false
    ) { }
}
