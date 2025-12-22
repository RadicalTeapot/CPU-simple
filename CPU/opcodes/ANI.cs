using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ANI, OpcodeGroupBaseCode.SingleRegisterLogicOne, RegisterArgsCount.One, OperandType.Immediate)]
    internal class ANI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var immediateValue = args.ImmediateValue;
            var value = (byte)(currentValue & immediateValue);
            cpuState.SetRegister(args.LowRegisterIdx, value);
            cpuState.SetZeroFlag(value == 0);
        }
    }
}
