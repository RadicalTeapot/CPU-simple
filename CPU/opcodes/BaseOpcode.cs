namespace CPU.opcodes
{
    public enum Opcode : byte
    {
        NOP = 0x00,
        MOV = 0x10,
        LDI = 0x20,
        HLT = 0xF0
    }

    internal abstract class BaseOpcode
    {
        public byte Size { get; }

        public BaseOpcode(byte size, Opcode code)
        {
            Size = size;
            _code = code;
        }

        public void Register(Dictionary<Opcode, BaseOpcode> dic)
        {
            dic.Add(_code, this);
        }

        public abstract void Execute(State state, byte[] args, Trace trace);

        protected void ValidateArgs(byte[] args, string instructionName)
        {
            if (args.Length != Size)
            {
                throw new ArgumentException($"Invalid number of arguments for instruction {instructionName}. Expected {Size}, got {args.Length}");
            }
        }

        private readonly Opcode _code;
    }
}
