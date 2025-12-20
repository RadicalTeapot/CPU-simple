using CPU.components;

namespace CPU.opcodes
{
    internal class PEK(State cpuState, Memory memory, Stack stack) : BaseOpcode(
        OpcodeBaseCode.PEK, RegisterArgsCount.One, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(PEK),
                Args = $"RD: {args.FirstRegisterId}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };
            var value = stack.PeekByte();
            CpuState.SetRegister(args.FirstRegisterId, value);
            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}
