using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CLC, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.None)]
    internal class CLC(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            CpuState.SetCarryFlag(false);
        }
    }
}
