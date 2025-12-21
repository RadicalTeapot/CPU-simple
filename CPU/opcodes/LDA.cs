using CPU.components;

namespace CPU.opcodes
{
    internal class LDA(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.LDA, RegisterArgsCount.One, OperandType.Address,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(LDA),
                Args = $"RD: {args.LowRegisterIdx}, ADDR: {args.AddressValue}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            CpuState.SetRegister(args.LowRegisterIdx, Memory.ReadByte(args.AddressValue));
            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}