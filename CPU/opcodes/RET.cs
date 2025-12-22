using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RET, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class RET(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var returnAddress = stack.PopAddress();
            cpuState.SetPC(returnAddress);
        }
    }
}