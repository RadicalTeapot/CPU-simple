using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SEC, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class SEC(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            cpuState.SetCarryFlag(true);
        }
    }
}
