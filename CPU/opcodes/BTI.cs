using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.BTI, OpcodeGroupBaseCode.SingleRegisterLogicOne, RegisterArgsCount.One, OperandType.Immediate)]
    internal class BTI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var immediateValue = args.ImmediateValue;
            var test = (byte)(currentValue & immediateValue);
            cpuState.SetZeroFlag(test != 0); // Test if any bit matches and value is not zero
        }
    }
}
