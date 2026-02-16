using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.HLT, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class HLT(byte instructionByte, State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public MicroPhase Tick(int phaseCount)
        {
            throw new OpcodeException.HaltException();
        }
    }
}