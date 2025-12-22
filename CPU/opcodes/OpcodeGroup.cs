namespace CPU.opcodes
{
    /// <summary>
    /// Enum defining the opcode group base codes (upper nibble of instruction byte).
    /// </summary>
    internal enum OpcodeGroupBaseCode : byte
    {
        SystemAndJump = 0x00,
        Load = 0x10,
        Store = 0x20,
        Move = 0x30,
        SingleRegisterALU = 0x40,
        Add = 0x50,
        Subtract = 0x60,
        BitsManipulation = 0x70,
        SingleRegisterLogicOne = 0x80,
        TwoRegistersCompare = 0x90,
        And = 0xA0,
        SingleRegisterLogicTwo = 0xB0,
        Or = 0xC0,
        Xor = 0xD0,
        AtomicAndBitTests = 0xE0,
        ExtendedOperations = 0xF0,
    }

    /// <summary>
    /// Static class providing masks for opcode parsing based on opcode groups.
    /// </summary>
    /// <remarks>
    /// Groups are used to determine the mask for extracting the opcode base code.
    /// The mask depends on how many register bits are encoded in the instruction.
    /// </remarks>
    internal static class OpcodeGroupMasks
    {
        public static Dictionary<OpcodeGroupBaseCode, byte> Mask = new()
        {
            [OpcodeGroupBaseCode.SystemAndJump] = NO_REGISTER_MASK,
            [OpcodeGroupBaseCode.Load] = ONE_REGISTER_MASK,
            [OpcodeGroupBaseCode.Store] = ONE_REGISTER_MASK,
            [OpcodeGroupBaseCode.Move] = TWO_REGISTER_MASK,
            [OpcodeGroupBaseCode.SingleRegisterALU] = ONE_REGISTER_MASK,
            [OpcodeGroupBaseCode.Add] = TWO_REGISTER_MASK,
            [OpcodeGroupBaseCode.Subtract] = TWO_REGISTER_MASK,
            [OpcodeGroupBaseCode.BitsManipulation] = ONE_REGISTER_MASK,
            [OpcodeGroupBaseCode.SingleRegisterLogicOne] = ONE_REGISTER_MASK,
            [OpcodeGroupBaseCode.TwoRegistersCompare] = TWO_REGISTER_MASK,
            [OpcodeGroupBaseCode.And] = TWO_REGISTER_MASK,
            [OpcodeGroupBaseCode.SingleRegisterLogicTwo] = ONE_REGISTER_MASK,
            [OpcodeGroupBaseCode.Or] = TWO_REGISTER_MASK,
            [OpcodeGroupBaseCode.Xor] = TWO_REGISTER_MASK,
            [OpcodeGroupBaseCode.AtomicAndBitTests] = ONE_REGISTER_MASK,
            [OpcodeGroupBaseCode.ExtendedOperations] = NO_REGISTER_MASK,
        };

        private const byte NO_REGISTER_MASK = 0xFF;     // No bits
        private const byte ONE_REGISTER_MASK = 0xFC;    // 2 bits for 1 register
        private const byte TWO_REGISTER_MASK = 0xF0;    // 4 bits for 2 registers
    }
}
