using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CLZ, OpcodeGroupBaseCode.SystemAndJump)]
    internal class CLZ(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(int phaseCount)
        {
            state.SetZeroFlag(false);
            return MicroPhase.Done;
        }
    }
}
