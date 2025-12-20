using CPU.components;

namespace CPU.opcodes
{
    // Note: This doesn't inherit from BaseOpcode because it has to handle PC differently
    internal class RET(State cpuState, Stack stack) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.RET] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            var returnAddress = stack.PopAddress();
            cpuState.SetPC(returnAddress);

            trace = new Trace()
            {
                InstructionName = nameof(RET),
                Args = $"ADDR: {returnAddress}",
                PcBefore = pcBefore,
                PcAfter = returnAddress
            };
        }
    }
}