using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDI, OpcodeGroupBaseCode.LOAD, RegisterArgsCount.One, OperandType.Immediate)]
    internal class LDI(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            CpuState.SetRegister(args.LowRegisterIdx, args.ImmediateValue);
        }
    }
}