using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SEZ, OpcodeGroupBaseCode.SystemAndJump)]
    internal class SEZ(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(int phaseCount)
        {
            state.SetZeroFlag(true);
            return MicroPhase.Done;
        }
    }
}
