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
                Args = $"RD: {args.LowRegisterIdx}, IMM: {immediateValue}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx)],
            };

            CpuState.SetRegister(args.LowRegisterIdx, immediateValue);

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx)];
            return trace;
        }
    }
}