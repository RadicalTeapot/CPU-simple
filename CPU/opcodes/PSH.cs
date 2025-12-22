using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.PSH, OpcodeGroupBaseCode.STORE, RegisterArgsCount.One, OperandType.None)]
    internal class PSH(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            var value = CpuState.GetRegister(args.LowRegisterIdx);
            Stack.PushByte(value);
        }
    }
}
