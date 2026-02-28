using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.NOP, OpcodeGroupBaseCode.SystemAndJump)]
    internal class NOP(byte instructionByte, State state, IBus bus, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(uint phaseCount) => MicroPhase.Done;
    }
}