using CPU.components;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;

namespace CPU.opcodes
{
    public enum Opcode : byte
    {
        NOP = 0x00,
        HLT = 0x01,
        LDI = 0x10,
        LDR = 0x14,
        STR = 0x24,
        MOV = 0x30,
        ADI = 0x40,
    }

    internal class OpcodeTable
    {
        public OpcodeTable(State cpuState, Stack stack, Memory memory)
        {
            _opcodes = new Dictionary<Opcode, IOpcode>
            {
                { Opcode.NOP, new NOP() },
                { Opcode.HLT, new HLT() },
                { Opcode.MOV, new MOV(cpuState, memory) },
                { Opcode.LDI, new LDI(cpuState, memory) },
                { Opcode.LDR, new LDR(cpuState, memory) },
                { Opcode.STR, new STR(cpuState, memory) },
                { Opcode.ADI, new ADI(cpuState, memory) },
            };

            var fullResolution = new Func<byte, byte>(value => value);
            var quarterResolution = new Func<byte, byte>(value => (byte)(value & OPCODE_MASK));
            var sixteenthResolution = new Func<byte, byte>(value => (byte)(value & GROUP_MASK));
            _groupOpcodeByteParsers = new Dictionary<OpcodeGroup, Func<byte, byte>>
            {
                { OpcodeGroup.SYSTEM_AND_JUMP, fullResolution },
                { OpcodeGroup.LOAD, quarterResolution },
                { OpcodeGroup.STORE, quarterResolution },
                { OpcodeGroup.MOVE, sixteenthResolution },
                { OpcodeGroup.SINGLE_REGISTER_ALU, quarterResolution },
            };
        }

        public IOpcode GetOpcode(byte instruction)
        {
            var opcode = ParseInstructionAsOpcode(instruction);

            if (!_opcodes.TryGetValue(opcode, out var op))
            {
                throw new KeyNotFoundException($"Invalid instruction: {instruction:X2}");
            }
            return op;
        }

        private Opcode ParseInstructionAsOpcode(byte instruction)
        {
            var opcodeGroupByte = (byte)(instruction & GROUP_MASK);
            if (!Enum.IsDefined(typeof(OpcodeGroup), opcodeGroupByte))
            {
                throw new KeyNotFoundException($"Unkown opcode group byte: {opcodeGroupByte:X2} (instruction was {instruction:X2})");
            }
            if (!_groupOpcodeByteParsers.TryGetValue((OpcodeGroup)opcodeGroupByte, out var instructionParser))
            {
                throw new KeyNotFoundException($"Un-registered opcode group: {opcodeGroupByte:X2} (instruction was {instruction:X2})");
            }

            var opcodeByte = instructionParser(instruction);
            if (!Enum.IsDefined(typeof(Opcode), opcodeByte))
            {
                throw new KeyNotFoundException($"Unknown opcode byte: {opcodeByte:X2}");
            }
            return (Opcode)opcodeByte;
        }

        private enum OpcodeGroup : byte
        {
            SYSTEM_AND_JUMP = 0x00,
            LOAD = 0x10,
            STORE = 0x20,
            MOVE = 0x30,
            SINGLE_REGISTER_ALU = 0x40,
        }

        private const byte GROUP_MASK = 0xF0;
        private const byte OPCODE_MASK = 0xFC;
        private readonly Dictionary<Opcode, IOpcode> _opcodes;
        private readonly Dictionary<OpcodeGroup, Func<byte, byte>> _groupOpcodeByteParsers;
    }
}
