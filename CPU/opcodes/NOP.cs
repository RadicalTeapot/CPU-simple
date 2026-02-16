using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.NOP, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class NOP(byte instructionByte, State cpuState, Memory memory, Stack stack) : IOpcode
    {
        public MicroPhase Tick(int phaseCount)
        {
            return MicroPhase.Done;
        }
    }
}