using CPU.components;

namespace CPU.opcodes
{
    internal class CMP(State cpuState, Memory memory) : BaseOpcode(
        OpcodeBaseCode.CMP, RegisterArgsCount.Two, OperandType.None,
        cpuState, memory)
    {
        protected override Trace Execute(OpcodeArgs args)
        {
            var trace = new Trace()
            {
                InstructionName = nameof(CMP),
                Args = $"RD: {args.FirstRegisterId}, RS: {args.SecondRegisterId}",
                RBefore = [CpuState.GetRegister(args.FirstRegisterId), CpuState.GetRegister(args.SecondRegisterId)],
            };

            var destination = CpuState.GetRegister(args.FirstRegisterId);
            var source = CpuState.GetRegister(args.SecondRegisterId);
            CpuState.SetZeroFlag(destination == source);
            CpuState.SetCarryFlag(destination >= source); // Similar to SUB (no borrow), but without actual subtraction

            trace.RAfter = [CpuState.GetRegister(args.FirstRegisterId), CpuState.GetRegister(args.SecondRegisterId)];
            return trace;
        }
    }
}
