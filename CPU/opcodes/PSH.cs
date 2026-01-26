using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.PSH, OpcodeGroupBaseCode.StoreAndIndirect, RegisterArgsCount.One, OperandType.None)]
    internal class PSH(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var value = cpuState.GetRegister(args.LowRegisterIdx);
            stack.PushByte(value, executionContext);
        }
    }
}
