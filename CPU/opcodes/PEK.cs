using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.PEK, OpcodeGroupBaseCode.Load, RegisterArgsCount.One, OperandType.None)]
    internal class PEK(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            var value = stack.PeekByte();
            cpuState.SetRegister(args.LowRegisterIdx, value);
        }
    }
}
