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
        STA = 0x24,
        MOV = 0x30,
        ADI = 0x40,
        SBI = 0x48,
        CMP = 0x90,
    }

    public interface IOpcode
    {
        void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry);
        void Execute(out Trace trace);
    }
}