using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CLC, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class CLC(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            cpuState.SetCarryFlag(false);
        }
    }
}
