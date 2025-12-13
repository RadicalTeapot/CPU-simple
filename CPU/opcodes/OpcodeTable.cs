using CPU.components;

namespace CPU.opcodes
{
    public enum Opcode : byte
    {
        NOP = 0x00,
        MOV = 0x10,
        LDI = 0x20,
        LDR = 0x30,
        STR = 0x40,
        HLT = 0xF0
    }

    internal class OpcodeTable
    {
        public OpcodeTable(State cpuState, Stack stack, Memory memory)
        {
            _opcodes = new Dictionary<Opcode, IOpcode>
            {
                { Opcode.NOP, new NOP() },
                { Opcode.MOV, new MOV(cpuState, memory) },
                { Opcode.LDI, new LDI(cpuState, memory) },
                { Opcode.LDR, new LDR(cpuState, memory) },
                { Opcode.STR, new STR(cpuState, memory) },
                { Opcode.HLT, new HLT() }
            };
        }

        public IOpcode GetOpcode(byte instruction)
        {
            if (_opcodes.TryGetValue((Opcode)(instruction & 0xF0), out var op))
            {
                return op;
            }
            throw new KeyNotFoundException($"Invalid instruction: {instruction:X2}");
        }

        private readonly Dictionary<Opcode, IOpcode> _opcodes;
    }
}
