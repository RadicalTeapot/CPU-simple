using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.HLT, OpcodeGroupBaseCode.SystemAndJump)]
    internal class HLT(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase Tick(int phaseCount)
        {
            throw new OpcodeException.HaltException();
        }
    }
}