using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADA, OpcodeGroupBaseCode.SingleRegisterALU, RegisterArgsCount.One, OperandType.Address)]
    internal class ADA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var memoryValue = memory.ReadByte(args.AddressValue);
            var result = currentValue + memoryValue + cpuState.GetCarryFlagAsInt();
            cpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            cpuState.SetCarryFlag(result > 0xFF);
            cpuState.SetZeroFlag(result == 0);
        }
    }
}
