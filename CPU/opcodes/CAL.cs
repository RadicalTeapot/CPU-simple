using CPU.components;

namespace CPU.opcodes
{
    internal class CAL(State cpuState, Memory memory, Stack stack) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.CAL] = this;

        public void Execute(out Trace trace)
        {
            var targetAddress = memory.ReadByte(cpuState.PC + 1);
            trace = new Trace()
            {
                InstructionName = nameof(CAL),
                Args = $"ADDR: {targetAddress}",
            };
            
            var returnAddress = (byte)(cpuState.PC + 2);
            stack.Push(returnAddress);

            cpuState.SetPC(targetAddress);
        }
    }
}