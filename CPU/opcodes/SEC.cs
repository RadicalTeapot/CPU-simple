using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SEC, OpcodeGroupBaseCode.SystemAndJump)]
    internal class SEC(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(uint phaseCount)
        {
            state.SetCarryFlag(true);
            return MicroPhase.Done;
        }
    }
}
