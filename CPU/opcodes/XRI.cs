using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.XRI, OpcodeGroupBaseCode.SingleRegisterLogicOne, RegisterArgsCount.One, OperandType.Immediate)]
    internal class XRI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var immediateValue = args.ImmediateValue;
            var value = (byte)(currentValue ^ immediateValue);
            cpuState.SetRegister(args.LowRegisterIdx, value);
            cpuState.SetZeroFlag(value == 0);
        }
    }
}
