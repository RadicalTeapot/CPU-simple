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
            return (byte)((instructionByte & HIGH_REGISTER_MASK) >> 3);
        }

        private const byte LOW_REGISTER_MASK = 0b111;
        private const byte HIGH_REGISTER_MASK = 0b111000;
    }
}
