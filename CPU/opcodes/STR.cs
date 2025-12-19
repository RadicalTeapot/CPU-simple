using CPU.components;

namespace CPU.opcodes
{
    internal class STR(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.STR, RegisterArgsCount.One, OperandType.Address,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(STR),
                Args = $"RS: {args.FirstRegisterId}, Memory address: {args.AddressValue}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };

            Memory.WriteByte(args.AddressValue, CpuState.GetRegister(args.FirstRegisterId));

            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}