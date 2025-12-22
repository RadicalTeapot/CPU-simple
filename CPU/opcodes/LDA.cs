using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDA, OpcodeGroupBaseCode.LOAD, RegisterArgsCount.One, OperandType.Address)]
    internal class LDA(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            CpuState.SetRegister(args.LowRegisterIdx, Memory.ReadByte(args.AddressValue));
        }
    }
}