using CPU.components;

namespace CPU.opcodes
{
    internal class CLC(State cpuState) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.CLC] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.SetCarryFlag(false);
            cpuState.IncrementPC();

            trace = new Trace()
            {
                InstructionName = nameof(CLC),
                Args = "-",
            };
        }
    }
}
