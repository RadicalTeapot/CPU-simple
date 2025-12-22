using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADA, OpcodeGroupBaseCode.SingleRegisterALU, RegisterArgsCount.One, OperandType.Immediate)]
    internal class CPI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var immediateValue = args.ImmediateValue;
            cpuState.SetCarryFlag(currentValue >= immediateValue); // Similar to SUB (no borrow), but without actual subtraction
            cpuState.SetZeroFlag(currentValue == immediateValue);
        }
    }
}
