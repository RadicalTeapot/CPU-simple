using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.STA, OpcodeGroupBaseCode.STORE, RegisterArgsCount.One, OperandType.Address)]
    internal class STA(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            Memory.WriteByte(args.AddressValue, CpuState.GetRegister(args.LowRegisterIdx));
        }
    }
}