using CPU.components;

namespace CPU.opcodes
{
    internal class POP(State cpuState, Memory memory, Stack stack) : BaseOpcode(
        OpcodeBaseCode.POP, RegisterArgsCount.One, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(POP),
                Args = $"RD: {args.FirstRegisterId}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };
            var value = stack.PopByte();
            CpuState.SetRegister(args.FirstRegisterId, value);
            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}
