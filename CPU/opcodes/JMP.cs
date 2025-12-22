using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JMP, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JMP(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetPC(args.AddressValue);
        }
    }
}