using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ORA, OpcodeGroupBaseCode.SingleRegisterLogicOne, RegisterArgsCount.One, OperandType.Address)]
    internal class ORA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var valueAtAddress = memory.ReadByte(args.AddressValue);
            var value = (byte)(currentValue | valueAtAddress);
            cpuState.SetRegister(args.LowRegisterIdx, value);
            cpuState.SetZeroFlag(value == 0);
        }
    }
}
