using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CLI, OpcodeGroupBaseCode.SystemAndJump)]
    internal class CLIOpcode(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(uint phaseCount)
        {
            state.SetInterruptDisableFlag(false);
            return MicroPhase.Done;
        }
    }
}
