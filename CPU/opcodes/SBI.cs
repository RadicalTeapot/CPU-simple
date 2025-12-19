using CPU.components;

namespace CPU.opcodes
{
    internal class SBI(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.SBI, RegisterArgsCount.One, OperandType.Immediate,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(SBI),
                Args = $"RD: {args.FirstRegisterId}, IMM: {args.ImmediateValue}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };

            var currentValue = CpuState.GetRegister(args.FirstRegisterId);
            var result = currentValue - args.ImmediateValue - (1 - CpuState.GetCarryFlagAsInt());
            CpuState.SetRegister(args.FirstRegisterId, (byte)result); // Wrap around on underflow
            CpuState.SetCarryFlag(result >= 0); // No borrow carry
            CpuState.SetZeroFlag(result == 0);

            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}