using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LSH, OpcodeGroupBaseCode.BitsManipulation, RegisterArgsCount.One, OperandType.None)]
    internal class LSH(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var value = cpuState.GetRegister(args.LowRegisterIdx);
            cpuState.SetRegister(args.LowRegisterIdx, (byte)(value << 1));
            cpuState.SetCarryFlag((value & 0x80) == 0x80); // Set carry flag to the bit that was shifted out (bit 7 of the original value)
        }
    }
}
