namespace CPU.opcodes
{
    /// <summary>
    /// Enum defining the opcode group base codes (upper nibble of instruction byte).
    /// </summary>
    /// <remarks>
    /// Groups are used to determine the mask for extracting the opcode base code.
    /// The mask depends on how many register bits are encoded in the instruction.
    /// </remarks>
    internal enum OpcodeGroupBaseCode : byte
    {
        SYSTEM_AND_JUMP = 0x00,         // Mask 0xFF: no register bits
        LOAD = 0x10,                    // Mask 0xFC: 2 register bits (1 register)
        STORE = 0x20,                   // Mask 0xFC: 2 register bits (1 register)
        MOVE = 0x30,                    // Mask 0xF0: 4 register bits (2 registers)
        SINGLE_REGISTER_ALU = 0x40,     // Mask 0xFC: 2 register bits (1 register)
        ADD = 0x50,                     // Mask 0xF0: 4 register bits (2 registers)
        SUB = 0x60,                     // Mask 0xF0: 4 register bits (2 registers)
        BITS_MANIPULATION = 0x70,       // Mask 0xFC: 2 register bits (1 register)
        TWO_REGISTERS_COMPARE = 0x90,   // Mask 0xF0: 4 register bits (2 registers)
    }

    internal static class OpcodeGroupMasks
    {
        public static Dictionary<OpcodeGroupBaseCode, byte> Mask = new()
        {
            [OpcodeGroupBaseCode.SYSTEM_AND_JUMP] = 0xFF,       // Full byte (no register args)
            [OpcodeGroupBaseCode.LOAD] = 0xFC,                  // 2 bits for 1 register
            [OpcodeGroupBaseCode.STORE] = 0xFC,                 // 2 bits for 1 register
            [OpcodeGroupBaseCode.MOVE] = 0xF0,                  // 4 bits for 2 registers
            [OpcodeGroupBaseCode.SINGLE_REGISTER_ALU] = 0xFC,   // 2 bits for 1 register
            [OpcodeGroupBaseCode.ADD] = 0xF0,                   // 4 bits for 2 registers
            [OpcodeGroupBaseCode.SUB] = 0xF0,                   // 4 bits for 2 registers
            [OpcodeGroupBaseCode.BITS_MANIPULATION] = 0xFC,     // 2 bits for 1 register
            [OpcodeGroupBaseCode.TWO_REGISTERS_COMPARE] = 0xF0, // 4 bits for 2 registers
        };
    }
}
