using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CPA, OpcodeGroupBaseCode.SingleRegisterALU, RegisterArgsCount.One, OperandType.Address)]
    internal class CPA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var valueAtAddress = memory.ReadByte(args.AddressValue);
            cpuState.SetCarryFlag(currentValue >= valueAtAddress); // Similar to SUB (no borrow), but without actual subtraction
            cpuState.SetZeroFlag(currentValue == valueAtAddress);
        }
    }
}
