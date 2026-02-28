using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CLC, OpcodeGroupBaseCode.SystemAndJump)]
    internal class CLC(byte instructionByte, State state, IBus bus, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(uint phaseCount)
        {
            state.SetCarryFlag(false);
            return MicroPhase.Done;
        }
    }
}
