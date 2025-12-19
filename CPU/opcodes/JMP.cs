using CPU.components;

namespace CPU.opcodes
{
    internal class JMP(State cpuState, Memory memory) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.JMP] = this;

        public void Execute(out Trace trace)
        {
            var targetAddress = memory.ReadByte(cpuState.PC + 1);
            trace = new Trace()
            {
                InstructionName = nameof(JMP),
                Args = $"ADDR: {targetAddress}",
            };
            cpuState.SetPC(targetAddress);
        }
    }
}