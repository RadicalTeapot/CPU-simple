using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDX, OpcodeGroupBaseCode.StoreAndIndirect, RegisterArgsCount.One, OperandType.RegAndImmediate)]
    internal class LDX(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var immediateValue = args.ImmediateValue;
            var registerValue = cpuState.GetRegister(args.LowRegisterIdx);
            var indirectRegisterValue = cpuState.GetRegister(args.IndirectRegisterIdx);
            var effectiveAddress = (byte)(indirectRegisterValue + immediateValue);
            var value = memory.ReadByte(effectiveAddress);
            cpuState.SetRegister(args.LowRegisterIdx, value);
        }
    }
}
