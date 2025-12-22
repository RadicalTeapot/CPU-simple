using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADA, OpcodeGroupBaseCode.SINGLE_REGISTER_ALU, RegisterArgsCount.One, OperandType.Address)]
    internal class ADA(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var currentValue = CpuState.GetRegister(args.LowRegisterIdx);
            var memoryValue = Memory.ReadByte(args.AddressValue);
            var result = currentValue + memoryValue + CpuState.GetCarryFlagAsInt();
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetZeroFlag(result == 0);
        }
    }
}
