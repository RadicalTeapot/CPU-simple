using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JCC, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JCC(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            if (!CpuState.C)
            {
                CpuState.SetPC(args.AddressValue);
            }
        }
    }
}
