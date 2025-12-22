using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SEZ, OpcodeGroupBaseCode.SystemAndJump, RegisterArgsCount.Zero, OperandType.None)]
    internal class SEZ(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetZeroFlag(true);
        }
    }
}
