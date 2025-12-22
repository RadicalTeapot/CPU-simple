using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.POP, OpcodeGroupBaseCode.Load, RegisterArgsCount.One, OperandType.None)]
    internal class POP(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var value = stack.PopByte();
            cpuState.SetRegister(args.LowRegisterIdx, value);
        }
    }
}
