using CPU.components;

namespace CPU.opcodes
{
    // Note: This doesn't inherit from BaseOpcode because it has to handle PC differently
    internal class JZS(State cpuState, Memory memory) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.JZS] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.IncrementPC(); // Move to operand
            var targetAddress = memory.ReadAddress(cpuState.GetPC(), out var size);

            if (!cpuState.Z)
                cpuState.IncrementPC(size);     // If condition not met, skip the jump address
            else
                cpuState.SetPC(targetAddress);  // Otherwise, perform the jump

            trace = new Trace()
            {
                InstructionName = nameof(JZS),
                Args = $"ADDR: {targetAddress}",
                PcBefore = pcBefore,
                PcAfter = cpuState.GetPC(),
            };
        }
    }
}
