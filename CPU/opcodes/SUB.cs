using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SUB, OpcodeGroupBaseCode.SUB, RegisterArgsCount.Two, OperandType.None)]
    internal class SUB(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var firstValue = CpuState.GetRegister(args.HighRegisterIdx);
            var secondValue = CpuState.GetRegister(args.LowRegisterIdx);
            var result = secondValue - firstValue - (1 - CpuState.GetCarryFlagAsInt());
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on underflow
            CpuState.SetCarryFlag(result >= 0); // No borrow carry
            CpuState.SetZeroFlag(result == 0);
        }
    }
}
