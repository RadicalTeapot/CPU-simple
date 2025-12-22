using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LSH, OpcodeGroupBaseCode.BITS_MANIPULATION, RegisterArgsCount.One, OperandType.None)]
    internal class LSH(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var value = CpuState.GetRegister(args.LowRegisterIdx);
            CpuState.SetRegister(args.LowRegisterIdx, (byte)(value << 1));
            CpuState.SetCarryFlag((value & 0x80) == 0x80); // Set carry flag to the bit that was shifted out (bit 7 of the original value)
        }
    }
}
