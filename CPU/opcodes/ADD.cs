using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADD, OpcodeGroupBaseCode.ADD, RegisterArgsCount.Two, OperandType.None)]
    internal class ADD(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var firstValue = CpuState.GetRegister(args.HighRegisterIdx);
            var secondValue = CpuState.GetRegister(args.LowRegisterIdx);
            var result = firstValue + secondValue + CpuState.GetCarryFlagAsInt();
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetZeroFlag(result == 0);
        }
    }
}
