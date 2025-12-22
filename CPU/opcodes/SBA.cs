using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SBA, OpcodeGroupBaseCode.SINGLE_REGISTER_ALU, RegisterArgsCount.One, OperandType.Address)]
    internal class SBA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var memoryValue = memory.ReadByte(args.AddressValue);
            var result = currentValue - memoryValue - (1 - cpuState.GetCarryFlagAsInt());
            cpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            cpuState.SetCarryFlag(result >= 0); // No borrow carry
            cpuState.SetZeroFlag(result == 0);
        }
    }
}
