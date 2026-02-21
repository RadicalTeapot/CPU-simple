using CPU.opcodes;
namespace CPU.microcode
{
    internal record MicrocodeTickResult(
        ulong TickCount,
        MicroPhase NextPhase,
        uint PhaseCount,
        OpcodeBaseCode CurrentOpcode,
        bool IsInstructionComplete = false
    ) { }
}
