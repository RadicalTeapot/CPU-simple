using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SBI, OpcodeGroupBaseCode.SINGLE_REGISTER_ALU, RegisterArgsCount.One, OperandType.Immediate)]
    internal class SBI(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var currentValue = CpuState.GetRegister(args.LowRegisterIdx);
            var result = currentValue - args.ImmediateValue - (1 - CpuState.GetCarryFlagAsInt());
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            CpuState.SetCarryFlag(result >= 0); // No borrow carry
            CpuState.SetZeroFlag(result == 0);
        }
    }
}