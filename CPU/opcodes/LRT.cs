using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LRT, OpcodeGroupBaseCode.BITS_MANIPULATION, RegisterArgsCount.One, OperandType.None)]
    internal class LRT(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var value = cpuState.GetRegister(args.LowRegisterIdx);
            var msb = (byte)(value & 0x80);
            cpuState.SetRegister(args.LowRegisterIdx, (byte)((value << 1) | (msb >> 7)));
        }
    }
}
