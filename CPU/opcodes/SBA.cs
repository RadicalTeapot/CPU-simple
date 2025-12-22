using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SBA, OpcodeGroupBaseCode.SINGLE_REGISTER_ALU, RegisterArgsCount.One, OperandType.Address)]
    internal class SBA(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var currentValue = CpuState.GetRegister(args.LowRegisterIdx);
            var memoryValue = Memory.ReadByte(args.AddressValue);
            var result = currentValue - memoryValue - (1 - CpuState.GetCarryFlagAsInt());
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            CpuState.SetCarryFlag(result >= 0); // No borrow carry
            CpuState.SetZeroFlag(result == 0);
        }
    }
}
