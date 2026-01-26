using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.JCC, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.Address)]
    internal class JCC(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            if (!cpuState.C)
            {
                cpuState.SetPC(args.AddressValue);
            }
        }
    }
}
