using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.PEK, OpcodeGroupBaseCode.LOAD, RegisterArgsCount.One, OperandType.None)]
    internal class PEK(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var value = Stack.PeekByte();
            CpuState.SetRegister(args.LowRegisterIdx, value);
        }
    }
}
