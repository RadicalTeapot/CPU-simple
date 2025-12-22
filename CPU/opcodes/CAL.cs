using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CAL, OpcodeGroupBaseCode.SYSTEM_AND_JUMP, RegisterArgsCount.Zero, OperandType.Address)]
    internal class CAL(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            // Push return address (current PC, which is already past the instruction and operand)
            var returnAddress = cpuState.GetPC();
            stack.PushAddress(returnAddress);

            // Jump to target
            cpuState.SetPC(args.AddressValue);
        }
    }
}