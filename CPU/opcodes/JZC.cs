using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JZC, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JZC(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            if (!CpuState.Z)
            {
                CpuState.SetPC(args.AddressValue);
            }
        }
    }
}
