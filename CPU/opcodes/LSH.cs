using CPU.components;

namespace CPU.opcodes
{
    internal class LSH(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.LSH, RegisterArgsCount.One, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(LDA),
                Args = $"RD: {args.LowRegisterIdx}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            var value = CpuState.GetRegister(args.LowRegisterIdx);
            CpuState.SetRegister(args.LowRegisterIdx, (byte)(value << 1));
            CpuState.SetCarryFlag((value & 0x80) == 0x80); // Set carry flag to the bit that was shifted out (bit 7 of the original value)

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}
