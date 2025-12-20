using CPU.components;

namespace CPU.opcodes
{
    internal class PSH(State cpuState, Memory memory, Stack stack) : BaseOpcode(
        OpcodeBaseCode.PSH, RegisterArgsCount.One, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(PSH),
                Args = $"RD: {args.FirstRegisterId}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };
            var value = CpuState.GetRegister(args.FirstRegisterId);
            stack.PushByte(value);
            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}
