using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.BTA, OpcodeGroupBaseCode.SingleRegisterLogicOne, RegisterArgsCount.One, OperandType.Address)]
    internal class BTA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var currentValue = cpuState.GetRegister(args.LowRegisterIdx);
            var valueAtAddress = memory.ReadByte(args.AddressValue);
            var test = (byte)(currentValue & valueAtAddress);
            cpuState.SetZeroFlag(test != 0); // Test if any bit matches and value is not zero
        }
    }
}
