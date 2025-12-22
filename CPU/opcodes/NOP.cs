using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.NOP, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.None)]
    internal class NOP(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            // No operation
        }
    }
}