namespace CPU.opcodes
{
    public class OpcodeException: Exception
    {
        public OpcodeException(string message) : base(message)
        {
        }

        public OpcodeException(string message, Exception inner) : base(message, inner)
        {
        }

        public class HaltException : OpcodeException
        {
            public HaltException() : base("HALT instruction executed.")
            {
            }
        }
    }
}
