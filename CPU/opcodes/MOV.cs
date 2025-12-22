using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.MOV, OpcodeGroupBaseCode.MOVE, RegisterArgsCount.Two, OperandType.None)]
    internal class MOV(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var value = CpuState.GetRegister(args.HighRegisterIdx);
            CpuState.SetRegister(args.LowRegisterIdx, value);
        }
    }
}