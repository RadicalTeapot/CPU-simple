using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CLZ, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class CLZ(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetZeroFlag(false);
        }
    }
}
