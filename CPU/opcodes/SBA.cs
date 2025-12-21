using CPU.components;

namespace CPU.opcodes
{
    internal class SBA(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.SBA, RegisterArgsCount.One, OperandType.Address,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(SBA),
                Args = $"RD: {args.LowRegisterIdx}, ADDR: {args.AddressValue}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            var currentValue = CpuState.GetRegister(args.LowRegisterIdx);
            var memoryValue = Memory.ReadByte(args.AddressValue);
            var result = currentValue - memoryValue - (1 - CpuState.GetCarryFlagAsInt());
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            CpuState.SetCarryFlag(result >= 0); // No borrow carry
            CpuState.SetZeroFlag(result == 0);

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}
