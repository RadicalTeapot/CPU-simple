using CPU.components;

namespace CPU.opcodes
{
    internal class NOP(State cpuState) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.NOP] = this;

        public void Execute(out Trace trace)
        {
            // No operation
            trace = new Trace
            {
                InstructionName = nameof(NOP),
                Args = "-",
            };
            cpuState.IncrementPC();
        }
    }
}