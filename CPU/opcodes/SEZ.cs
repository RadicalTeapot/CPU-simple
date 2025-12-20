using CPU.components;

namespace CPU.opcodes
{
    internal class SEZ(State cpuState) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.SEZ] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.SetZeroFlag(true);
            cpuState.IncrementPC();

            trace = new Trace()
            {
                InstructionName = nameof(SEZ),
                Args = "-",
            };
        }
    }
}
