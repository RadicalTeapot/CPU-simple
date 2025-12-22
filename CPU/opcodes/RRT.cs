using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RRT, OpcodeGroupBaseCode.BITS_MANIPULATION, RegisterArgsCount.One, OperandType.None)]
    internal class RRT(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var value = cpuState.GetRegister(args.LowRegisterIdx);
            var lsb = (byte)(value & 0x01);
            cpuState.SetRegister(args.LowRegisterIdx, (byte)((value >> 1) | (lsb << 7)));
        }
    }
}
