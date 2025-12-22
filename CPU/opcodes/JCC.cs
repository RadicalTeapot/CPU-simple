using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JCC, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JCC(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            if (!cpuState.C)
            {
                cpuState.SetPC(args.AddressValue);
            }
        }
    }
}
