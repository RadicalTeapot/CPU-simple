using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.PSH, OpcodeGroupBaseCode.STORE, RegisterArgsCount.One, OperandType.None)]
    internal class PSH(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var value = cpuState.GetRegister(args.LowRegisterIdx);
            stack.PushByte(value);
        }
    }
}
