using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SEZ, OpcodeGroupBaseCode.SystemAndJump)]
    internal class SEZ(byte instructionByte, State state, IBus bus, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(uint phaseCount)
        {
            state.SetZeroFlag(true);
            return MicroPhase.Done;
        }
    }
}
