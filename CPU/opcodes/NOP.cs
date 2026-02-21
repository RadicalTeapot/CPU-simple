using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.NOP, OpcodeGroupBaseCode.SystemAndJump)]
    internal class NOP(byte instructionByte, State state, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase Tick(int phaseCount) => MicroPhase.Done;
    }
}