using CPU.opcodes;
namespace CPU.microcode
{
    public record MicrocodeTickResult(
        ulong TickCount,
        MicroPhase NextPhase,
        uint PhaseCount,
        OpcodeBaseCode CurrentOpcode,
        bool IsInstructionComplete = false,
        TickTrace? Trace = null
    ) { }
}
