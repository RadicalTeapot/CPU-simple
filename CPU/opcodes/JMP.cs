using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JMP, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JMP(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetPC(args.AddressValue);
        }
    }
}