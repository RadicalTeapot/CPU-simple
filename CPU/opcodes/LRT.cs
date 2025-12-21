using CPU.components;

namespace CPU.opcodes
{
    internal class LRT(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.LRT, RegisterArgsCount.One, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(LRT),
                Args = $"RD: {args.LowRegisterIdx}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            var value = CpuState.GetRegister(args.LowRegisterIdx);
            var msb = (byte)(value & 0x80);
            CpuState.SetRegister(args.LowRegisterIdx, (byte)((value << 1) | (msb >> 7)));

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}
