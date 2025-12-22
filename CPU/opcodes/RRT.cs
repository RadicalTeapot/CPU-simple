using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RRT, OpcodeGroupBaseCode.BITS_MANIPULATION, RegisterArgsCount.One, OperandType.None)]
    internal class RRT(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var value = CpuState.GetRegister(args.LowRegisterIdx);
            var lsb = (byte)(value & 0x01);
            CpuState.SetRegister(args.LowRegisterIdx, (byte)((value >> 1) | (lsb << 7)));
        }
    }
}
