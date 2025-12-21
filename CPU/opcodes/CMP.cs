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
                Args = $"RD: {args.LowRegisterIdx}, RS: {args.HighRegisterIdx}",
                RBefore = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)],
            };

            var source = CpuState.GetRegister(args.HighRegisterIdx);
            var destination = CpuState.GetRegister(args.LowRegisterIdx);            
            CpuState.SetZeroFlag(destination == source);
            CpuState.SetCarryFlag(destination >= source); // Similar to SUB (no borrow), but without actual subtraction

            trace.RAfter = [CpuState.GetRegister(args.LowRegisterIdx), CpuState.GetRegister(args.HighRegisterIdx)];
            return trace;
        }
    }
}
