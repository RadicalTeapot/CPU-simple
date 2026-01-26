using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.HLT, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class HLT(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            throw new OpcodeException.HaltException();
        }
    }
}