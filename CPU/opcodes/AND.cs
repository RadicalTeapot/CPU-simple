using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.AND, OpcodeGroupBaseCode.And, RegisterArgsCount.Two, OperandType.None)]
    internal class AND(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var firstValue = cpuState.GetRegister(args.HighRegisterIdx);
            var secondValue = cpuState.GetRegister(args.LowRegisterIdx);
            var value = (byte)(firstValue & secondValue);
            cpuState.SetRegister(args.LowRegisterIdx, value);
            cpuState.SetZeroFlag(value == 0);
        }
    }
}
