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
                Args = $"RD: {args.FirstRegisterId}, RS: {args.SecondRegisterId}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId), CpuState.GetRegister(args.SecondRegisterId)],
            };

            var value = CpuState.GetRegister(args.SecondRegisterId);
            CpuState.SetRegister(args.FirstRegisterId, value);

            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId), CpuState.GetRegister(args.SecondRegisterId)];
            return trace;
        }
    }
}