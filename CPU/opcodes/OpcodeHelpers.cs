namespace CPU.opcodes
{
    internal static class OpcodeHelpers
    {
        public static byte GetLowRegisterIdx(byte instructionByte)
        {
            return (byte)(instructionByte & LOW_REGISTER_MASK);
        }

        public static byte GetHighRegisterIdx(byte instructionByte)
        {
            return (byte)((instructionByte & HIGH_REGISTER_MASK) >> 2);
        }

        private const byte LOW_REGISTER_MASK = 0b11;
        private const byte HIGH_REGISTER_MASK = 0b1100;
    }
}
