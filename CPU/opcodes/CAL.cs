using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CAL, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.Address)]
    internal class CAL(State cpuState, Memory memory, Stack stack) : BaseOpcode(cpuState, memory, stack)
    {
        public override void Execute(OpcodeArgs args)
        {
            // Push return address (current PC, which is already past the instruction and operand)
            var returnAddress = CpuState.GetPC();
            Stack.PushAddress(returnAddress);

            // Jump to target
            CpuState.SetPC(args.AddressValue);
        }
    }
}