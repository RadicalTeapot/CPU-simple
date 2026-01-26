using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.STX, OpcodeGroupBaseCode.StoreAndIndirect, RegisterArgsCount.One, OperandType.RegAndImmediate)]
    internal class STX(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var immediateValue = args.ImmediateValue;
            var indirectRegisterValue = cpuState.GetRegister(args.IndirectRegisterIdx);
            var registerValue = cpuState.GetRegister(args.LowRegisterIdx);
            var effectiveAddress = (byte)(indirectRegisterValue + immediateValue);
            memory.WriteByte(effectiveAddress, registerValue, executionContext);
        }
    }
}
