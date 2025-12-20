using CPU.components;

namespace CPU.opcodes
{
    // Note: This doesn't inherit from BaseOpcode because it has to handle PC differently
    internal class JMP(State cpuState, Memory memory) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.JMP] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.IncrementPC(1); // Move to operand
            var targetAddress = memory.ReadAddress(cpuState.GetPC(), out _);
            cpuState.SetPC(targetAddress);

            trace = new Trace()
            {
                InstructionName = nameof(JMP),
                Args = $"ADDR: {targetAddress}",
                PcBefore = pcBefore,
                PcAfter = targetAddress
            };
        }
    }
}