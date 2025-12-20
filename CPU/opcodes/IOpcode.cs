namespace CPU.opcodes
{
    public enum OpcodeBaseCode : byte
    {
        NOP = 0x00,
        HLT = 0x01,
        CLC = 0x02,
        JMP = 0x08,
        JCC = 0x0A,
        CAL = 0x0E,
        RET = 0x0F,
        LDI = 0x10,
        LDR = 0x14,
        STR = 0x24,
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