using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CMP, OpcodeGroupBaseCode.TWO_REGISTERS_COMPARE, RegisterArgsCount.Two, OperandType.None)]
    internal class CMP(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var source = CpuState.GetRegister(args.HighRegisterIdx);
            var destination = CpuState.GetRegister(args.LowRegisterIdx);            
            CpuState.SetZeroFlag(destination == source);
            CpuState.SetCarryFlag(destination >= source); // Similar to SUB (no borrow), but without actual subtraction
        }
    }
}
