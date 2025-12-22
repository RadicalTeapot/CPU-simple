using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDA, OpcodeGroupBaseCode.LOAD, RegisterArgsCount.One, OperandType.Address)]
    internal class LDA(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetRegister(args.LowRegisterIdx, memory.ReadByte(args.AddressValue));
        }
    }
}