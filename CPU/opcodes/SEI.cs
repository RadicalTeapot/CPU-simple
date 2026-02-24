using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SEI, OpcodeGroupBaseCode.SystemAndJump)]
    internal class SEI(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(uint phaseCount)
        {
            state.SetInterruptDisableFlag(true);
            return MicroPhase.Done;
        }
    }
}
