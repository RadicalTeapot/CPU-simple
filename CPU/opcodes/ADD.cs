using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADD, OpcodeGroupBaseCode.ADD, RegisterArgsCount.Two, OperandType.None)]
    internal class ADD(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var firstValue = cpuState.GetRegister(args.HighRegisterIdx);
            var secondValue = cpuState.GetRegister(args.LowRegisterIdx);
            var result = firstValue + secondValue + cpuState.GetCarryFlagAsInt();
            cpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            cpuState.SetCarryFlag(result > 0xFF);
            cpuState.SetZeroFlag(result == 0);
        }
    }
}