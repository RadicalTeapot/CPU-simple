using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JZS, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JZS(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            if (cpuState.Z)
            {
                cpuState.SetPC(args.AddressValue);
            }
        }
    }
}
