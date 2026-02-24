using CPU.opcodes;
namespace CPU.microcode
{
    public record MicrocodeTickResult(
        ulong TickCount,
        MicroPhase ExecutedPhase,
        MicroPhase NextPhase,
        uint PhaseCount,
        OpcodeBaseCode CurrentOpcode,
        bool IsInstructionComplete = false
    ) { }
}
