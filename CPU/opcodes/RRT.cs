using CPU.components;

namespace CPU.opcodes
{
    internal class RRT(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.RRT, RegisterArgsCount.One, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(RRT),
                Args = $"RD: {args.LowRegisterIdx}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            var value = CpuState.GetRegister(args.LowRegisterIdx);
            var lsb = (byte)(value & 0x01);
            CpuState.SetRegister(args.LowRegisterIdx, (byte)((value >> 1) | (lsb << 7)));

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}
