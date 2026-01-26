using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.NOP, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class NOP(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            // No operation
        }
    }
}