using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SBI, OpcodeGroupBaseCode.SingleRegisterALU, RegisterArgsCount.One, OperandType.Immediate)]
    internal class SBI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var result = currentValue - args.ImmediateValue - (1 - cpuState.GetCarryFlagAsInt());
            cpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            cpuState.SetCarryFlag(result >= 0); // No borrow carry
            cpuState.SetZeroFlag(result == 0);
        }
    }
}