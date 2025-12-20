using CPU.components;

namespace CPU.opcodes
{
    internal class SEC(State cpuState) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.SEC] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.SetCarryFlag(true);
            cpuState.IncrementPC();

            trace = new Trace()
            {
                InstructionName = nameof(SEC),
                Args = "-",
            };
        }
    }
}
