namespace CPU.opcodes
{
    internal class HLT: IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.HLT] = this;

        public void Execute(out Trace trace)
        {
            trace = new Trace
            {
                InstructionName = nameof(HLT),
                Args = "-",
            };
            throw new OpcodeException.HaltException();
        }
    }
}