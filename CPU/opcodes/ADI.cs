using CPU.components;

namespace CPU.opcodes
{
    internal class ADI(State cpuState, Memory memory): BaseOpcode(
        OpcodeBaseCode.ADI, RegisterArgsCount.One, OperandType.Immediate, 
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(ADI),
                Args = $"RD: {args.FirstRegisterId}, IMM: {args.ImmediateValue}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };

            var currentValue = CpuState.GetRegister(args.FirstRegisterId);
            var result = currentValue + args.ImmediateValue + CpuState.GetCarryFlagAsInt();
            CpuState.SetRegister(args.FirstRegisterId, (byte)result); // Wrap around on overflow
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetZeroFlag(result == 0);

            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}