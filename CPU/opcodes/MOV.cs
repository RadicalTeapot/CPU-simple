using CPU.components;

namespace CPU.opcodes
{
    internal class MOV(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.MOV, RegisterArgsCount.Two, OperandType.None, 
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(MOV),
                Args = $"RD: {args.LowRegisterIdx}, RS: {args.HighRegisterIdx}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)],
            };

            var value = CpuState.GetRegister(args.HighRegisterIdx);
            CpuState.SetRegister(args.LowRegisterIdx, value);

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)];
            return trace;
        }
    }
}