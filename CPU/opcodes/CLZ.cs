using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CLZ, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.None)]
    internal class CLZ(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetZeroFlag(false);
        }
    }
}
