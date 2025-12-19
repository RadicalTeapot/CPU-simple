using CPU.components;

namespace CPU.opcodes
{
    internal class LDI(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.LDI, RegisterArgsCount.One, OperandType.Immediate,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var immediateValue = args.ImmediateValue;
            
            var trace = new Trace()
            {
                InstructionName = nameof(LDI),
                Args = $"RD: {args.FirstRegisterId}, IMM: {immediateValue}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId)],
            };

            CpuState.SetRegister(args.FirstRegisterId, immediateValue);

            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId)];
            return trace;
        }
    }
}