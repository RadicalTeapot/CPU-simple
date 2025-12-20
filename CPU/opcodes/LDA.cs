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
                Args = $"RD: {args.FirstRegisterId}, ADDR: {args.AddressValue}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };

            CpuState.SetRegister(args.FirstRegisterId, Memory.ReadByte(args.AddressValue));
            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}