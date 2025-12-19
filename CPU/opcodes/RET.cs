using CPU.components;

namespace CPU.opcodes
{
    internal class RET(State cpuState, Stack stack) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.RET] = this;

        public void Execute(out Trace trace)
        {
            var returnAddress = stack.Pop();
            trace = new Trace()
            {
                InstructionName = nameof(RET),
                Args = $"ADDR: {returnAddress}",
            };
            cpuState.SetPC(returnAddress);
        }
    }
}