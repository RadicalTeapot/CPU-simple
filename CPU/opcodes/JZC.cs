using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JZC, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JZC(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            if (!cpuState.Z)
            {
                cpuState.SetPC(args.AddressValue);
            }
        }
    }
}
