using CPU.components;

namespace CPU.opcodes
{
    internal class ADA(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.ADA, RegisterArgsCount.One, OperandType.Address,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(ADA),
                Args = $"RD: {args.LowRegisterIdx}, ADDR: {args.AddressValue}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            var currentValue = CpuState.GetRegister(args.LowRegisterIdx);
            var memoryValue = Memory.ReadByte(args.AddressValue);
            var result = currentValue + memoryValue + CpuState.GetCarryFlagAsInt();
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetZeroFlag(result == 0);

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}
