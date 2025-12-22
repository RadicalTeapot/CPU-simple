using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADI, OpcodeGroupBaseCode.SINGLE_REGISTER_ALU, RegisterArgsCount.One, OperandType.Immediate)]
    internal class ADI(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var currentValue = CpuState.GetRegister(args.LowRegisterIdx);
            var result = currentValue + args.ImmediateValue + CpuState.GetCarryFlagAsInt();
            CpuState.SetRegister(args.LowRegisterIdx, (byte)result); // Wrap around on overflow
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetZeroFlag(result == 0);
        }
    }
}