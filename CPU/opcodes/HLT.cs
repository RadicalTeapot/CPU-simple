using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.HLT, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.None)]
    internal class HLT(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            throw new OpcodeException.HaltException();
        }
    }
}