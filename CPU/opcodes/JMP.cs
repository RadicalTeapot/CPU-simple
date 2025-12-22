using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JMP, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JMP(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            CpuState.SetPC(args.AddressValue);
        }
    }
}