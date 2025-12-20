using CPU.components;

namespace CPU.opcodes
{
    internal class CLZ(State cpuState) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.CLZ] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.SetZeroFlag(false);
            cpuState.IncrementPC();

            trace = new Trace()
            {
                InstructionName = nameof(CLZ),
                Args = "-",
            };
        }
    }
}
