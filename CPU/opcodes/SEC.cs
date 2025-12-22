using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SEC, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.None)]
    internal class SEC(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetCarryFlag(true);
        }
    }
}
