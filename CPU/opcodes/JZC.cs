using CPU.components;

namespace CPU.opcodes
{
    internal class JZC(State cpuState, Memory memory) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.JZC] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.IncrementPC(); // Move to operand
            var targetAddress = memory.ReadAddress(cpuState.GetPC(), out var size);

            if (cpuState.Z)
                cpuState.IncrementPC(size);     // If condition not met, skip the jump address
            else
                cpuState.SetPC(targetAddress);  // Otherwise, perform the jump

            trace = new Trace()
            {
                InstructionName = nameof(JMP),
                Args = $"ADDR: {targetAddress}",
                PcBefore = pcBefore,
                PcAfter = cpuState.GetPC(),
            };
        }
    }
}
