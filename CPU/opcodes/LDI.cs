using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDI, OpcodeGroupBaseCode.Load, RegisterArgsCount.One, OperandType.Immediate)]
    internal class LDI(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute(ExecutionContext executionContext)
        {
            cpuState.SetRegister(args.LowRegisterIdx, args.ImmediateValue);
        }
    }
}