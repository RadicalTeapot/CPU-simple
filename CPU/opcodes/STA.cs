using CPU.components;

namespace CPU.opcodes
{
    internal class STA(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.STA, RegisterArgsCount.One, OperandType.Address,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(STA),
                Args = $"RS: {args.LowRegisterIdx}, Memory address: {args.AddressValue}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            Memory.WriteByte(args.AddressValue, CpuState.GetRegister(args.LowRegisterIdx));

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}