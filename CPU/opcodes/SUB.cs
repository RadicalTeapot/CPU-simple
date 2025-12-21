using CPU.components;

namespace CPU.opcodes
{
    internal class SUB(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.SUB, RegisterArgsCount.Two, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(SUB),
                Args = $"RD: {args.LowRegisterIdx}, RS: {args.HighRegisterIdx}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)],
            };

            var firstValue = CpuState.GetRegister(args.HighRegisterIdx);
            var secondValue = CpuState.GetRegister(args.LowRegisterIdx);
            var result = secondValue - firstValue - (1 - CpuState.GetCarryFlagAsInt());
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            CpuState.SetCarryFlag(result >= 0); // No borrow carry
            CpuState.SetZeroFlag(result == 0);
            
            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)];
            return trace;
        }
    }
}
