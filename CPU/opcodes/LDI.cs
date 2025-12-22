using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDI, OpcodeGroupBaseCode.LOAD, RegisterArgsCount.One, OperandType.Immediate)]
    internal class LDI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            cpuState.SetRegister(args.LowRegisterIdx, args.ImmediateValue);
        }
    }
}