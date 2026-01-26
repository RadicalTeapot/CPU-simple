using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDA, OpcodeGroupBaseCode.Load, RegisterArgsCount.One, OperandType.Address)]
    internal class LDA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            cpuState.SetRegister(args.LowRegisterIdx, memory.ReadByte(args.AddressValue));
        }
    }
}