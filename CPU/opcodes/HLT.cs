using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.HLT, OpcodeGroupBaseCode.SystemAndJump)]
    internal class HLT(byte instructionByte, State state, IBus bus, Stack stack) : IOpcode
    {
        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

        public MicroPhase Tick(uint phaseCount)
        {
            throw new OpcodeException.HaltException();
        }
    }
}