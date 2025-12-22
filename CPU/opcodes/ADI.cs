using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADI, OpcodeGroupBaseCode.SINGLE_REGISTER_ALU, RegisterArgsCount.One, OperandType.Immediate)]
    internal class ADI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var result = currentValue + args.ImmediateValue + cpuState.GetCarryFlagAsInt();
            cpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            cpuState.SetCarryFlag(result > 0xFF);
            cpuState.SetZeroFlag(result == 0);
        }
    }
}