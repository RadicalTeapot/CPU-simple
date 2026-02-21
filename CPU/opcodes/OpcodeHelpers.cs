namespace CPU.opcodes
{
    internal static class OpcodeHelpers
    {
        /// <summary>
        /// Get the destination register index from the instruction byte.
        /// </summary>
        /// <param name="instructionByte">The instruction byte</param>
        /// <remarks>The destination register is encoded in bits 0 and 1 of the instruction byte.</remarks>
        /// <returns>The destination register index.</returns>
        public static byte GetDestinationRegisterIdx(byte instructionByte)
        {
            return (byte)(instructionByte & LOW_REGISTER_MASK);
        }

        /// <summary>
        /// Get the source register index from the instruction byte.
        /// </summary>
        /// <param name="instructionByte">The instruction byte</param>
        /// <remarks>The source register is encoded in bits 2 and 3 of the instruction byte.</remarks>
        /// <returns>The source register index.</returns>
        public static byte GetSourceRegisterIdx(byte instructionByte)
        {
            return (byte)((instructionByte & HIGH_REGISTER_MASK) >> 2);
        }

        private const byte LOW_REGISTER_MASK = 0b11;
        private const byte HIGH_REGISTER_MASK = 0b1100;
    }
}
