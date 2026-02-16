using CPU.microcode;

namespace CPU.opcodes
{
    public enum OpcodeBaseCode : byte
    {
        NOP = 0x00,
        HLT = 0x01,
        CLC = 0x02,
        SEC = 0x03,
        CLZ = 0x04,
        SEZ = 0x05,
        JMP = 0x08,
        JCC = 0x0A,
        JCS = 0x0B,
        JZC = 0x0C,
        JZS = 0x0D,
        CAL = 0x0E,
        RET = 0x0F,
        LDI = 0x10,
        LDA = 0x14,
        POP = 0x18,
        PEK = 0x1C,
        PSH = 0x20,
        STA = 0x24,
        LDX = 0x28,
        STX = 0x2C,
        MOV = 0x30,
        ADI = 0x40,
        ADA = 0x44,
        SBI = 0x48,
        SBA = 0x4C,
        ADD = 0x50,
        SUB = 0x60,
        LSH = 0x70,
        RSH = 0x74,
        LRT = 0x78,
        RRT = 0x7C,
        CPI = 0x80,
        CPA = 0x84,
        ANI = 0x88,
        ANA = 0x8C,
        CMP = 0x90,
        AND = 0xA0,
        ORI = 0xB0,
        ORA = 0xB4,
        XRI = 0xB8,
        XRA = 0xBC,
        OR  = 0xC0,
        XOR = 0xD0,
        INC = 0xE0,
        DEC = 0xE4,
        BTI = 0xE8,
        BTA = 0xEC,
    }

    /// <remarks>DEPRECATED</remarks>
    internal struct OpcodeArgs()
    {
        /// <summary>
        /// Bits 2-3 register index, typically source register, if applicable.
        /// </summary>
        public byte HighRegisterIdx = 0;
        /// <summary>
        /// Bits 0-1 register index, typically destination register, if applicable.
        /// </summary>
        public byte LowRegisterIdx = 0;
        public byte IndirectRegisterIdx = 0;
        public byte ImmediateValue = 0;
#if x16
        public ushort AddressValue = 0;
#else
        public byte AddressValue = 0;
#endif
    }

    /// <summary>
    /// Interface for CPU opcodes.
    /// </summary>
    /// <remarks>
    /// Opcodes should be decorated with <see cref="OpcodeAttribute"/> for auto-discovery.
    /// </remarks>
    internal interface IOpcode
    {
        /// <summary>
        /// Executes a single micro-instruction phase of the opcode.
        /// </summary>
        /// <param name="phaseCount">The current phase count.</param>
        /// <returns>The micro-phase result.</returns>
        /// <remarks>
        /// This method should not be called directly. It is invoked by the CPU's instruction execution pipeline.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if the phase count is out of range for instruction.</exception>
        MicroPhase Tick(int phaseCount);
    }
}