using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.STA, OpcodeGroupBaseCode.StoreAndIndirect, RegisterArgsCount.One, OperandType.Address)]
    internal class STA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            memory.WriteByte(args.AddressValue, cpuState.GetRegister(args.LowRegisterIdx), executionContext);
        }
    }
}