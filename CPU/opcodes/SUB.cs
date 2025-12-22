using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SUB, OpcodeGroupBaseCode.SUB, RegisterArgsCount.Two, OperandType.None)]
    internal class SUB(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var firstValue = cpuState.GetRegister(args.HighRegisterIdx);
            var secondValue = cpuState.GetRegister(args.LowRegisterIdx);
            var result = secondValue - firstValue - (1 - cpuState.GetCarryFlagAsInt());
            cpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            cpuState.SetCarryFlag(result >= 0); // No borrow carry
            cpuState.SetZeroFlag(result == 0);
        }
    }
}
