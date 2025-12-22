using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RSH, OpcodeGroupBaseCode.BitsManipulation, RegisterArgsCount.One, OperandType.None)]
    internal class RSH(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var value = cpuState.GetRegister(args.LowRegisterIdx);
            cpuState.SetRegister(args.LowRegisterIdx, (byte)(value >> 1));
            cpuState.SetCarryFlag((value & 0x01) == 0x01); // Set carry flag to the bit that was shifted out (bit 0 of the original value)
        }
    }
}
