using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LRT, OpcodeGroupBaseCode.BITS_MANIPULATION, RegisterArgsCount.One, OperandType.None)]
    internal class LRT(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var value = CpuState.GetRegister(args.LowRegisterIdx);
            var msb = (byte)(value & 0x80);
            CpuState.SetRegister(args.LowRegisterIdx, (byte)((value << 1) | (msb >> 7)));
        }
    }
}
