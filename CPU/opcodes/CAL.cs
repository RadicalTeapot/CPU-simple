using CPU.components;

namespace CPU.opcodes
{
    internal class CAL(State cpuState, Memory memory, Stack stack) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.CAL] = this;

        public void Execute(out Trace trace)
        {
            trace = new Trace()
            {
                InstructionName = nameof(CAL),
                PcBefore = cpuState.GetPC(),
            };

            cpuState.IncrementPC(); // Move to operand
            var targetAddress = memory.ReadAddress(cpuState.GetPC(), out var size);
            trace.Args = $"ADDR: {targetAddress}";

            cpuState.IncrementPC(size); // Move to next instruction
            var returnAddress = cpuState.GetPC();
            stack.PushAddress(returnAddress);

            cpuState.SetPC(targetAddress);

            trace.PcAfter = cpuState.GetPC();
        }
    }
}