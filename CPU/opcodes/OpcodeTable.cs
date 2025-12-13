namespace CPU.opcodes
{
    internal class OpcodeTable
    {
        public OpcodeTable()
        {
            new NOP().Register(opcodes);
            new MOV().Register(opcodes);
            new LDI().Register(opcodes);
            new HLT().Register(opcodes);
        }

        public BaseOpcode GetOpcode(byte instruction)
        {
            if (opcodes.TryGetValue((Opcode)(instruction & 0xF0), out var op))
            {
                return op;
            }
            throw new KeyNotFoundException($"Invalid instruction: {instruction:X2}");
        }

        private readonly Dictionary<Opcode, BaseOpcode> opcodes = [];
    }
}
