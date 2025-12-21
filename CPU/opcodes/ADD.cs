using CPU.components;

namespace CPU.opcodes
{
    internal class ADD(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.ADD, RegisterArgsCount.Two, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(ADD),
                Args = $"RD: {args.LowRegisterIdx}, RS: {args.HighRegisterIdx}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)],
            };

            var firstValue = CpuState.GetRegister(args.HighRegisterIdx);
            var secondValue = CpuState.GetRegister(args.LowRegisterIdx);
            var result = firstValue + secondValue + CpuState.GetCarryFlagAsInt();
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetZeroFlag(result == 0);

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)];
            return trace;
        }
    }
}
