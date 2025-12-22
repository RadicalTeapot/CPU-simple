using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.POP, OpcodeGroupBaseCode.LOAD, RegisterArgsCount.One, OperandType.None)]
    internal class POP(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var value = Stack.PopByte();
            CpuState.SetRegister(args.LowRegisterIdx, value);
        }
    }
}
