using CPU.components;

namespace CPU.opcodes
{
    internal class OpcodeFactory
    {
        public OpcodeFactory(State cpuState, Stack stack, Memory memory)
        {
            _opcodeGroupRegistry = [];
            // Register opcode groups
            new SystemAndJumpOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);
            new LoadOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);
            new StoreOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);
            new MoveOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);
            new SingleRegisterALUOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);

            _opcodes = [];
            // Register opcodes
            new NOP().RegisterOpcode(_opcodes);
            new HLT().RegisterOpcode(_opcodes);
            new LDI(cpuState, memory).RegisterOpcode(_opcodes);
            new LDR(cpuState, memory).RegisterOpcode(_opcodes);
            new STR(cpuState, memory).RegisterOpcode(_opcodes);
            new MOV(cpuState, memory).RegisterOpcode(_opcodes);
            new ADI(cpuState, memory).RegisterOpcode(_opcodes);
        }

        public IOpcode GetOpcodeFromInstruction(byte instruction)
        {
            var opcodeBaseCode = ExtractOpcodeBaseCodeFromInstruction(instruction);

            if (!_opcodes.TryGetValue(opcodeBaseCode, out var op))
            {
                throw new KeyNotFoundException($"Un-registered opcode base code: {opcodeBaseCode:X2} (instruction was {instruction:X2})");
            }
            return op;
        }

        private OpcodeBaseCode ExtractOpcodeBaseCodeFromInstruction(byte instruction)
        {
            var opcodeGroupByte = (byte)(instruction & GROUP_MASK);
            if (!Enum.IsDefined(typeof(OpcodeGroupBaseCode), opcodeGroupByte))
            {
                throw new KeyNotFoundException($"Unkown opcode group byte: {opcodeGroupByte:X2} (instruction was {instruction:X2})");
            }
            if (!_opcodeGroupRegistry.TryGetValue((OpcodeGroupBaseCode)opcodeGroupByte, out var opcodeGroup))
            {
                throw new KeyNotFoundException($"Un-registered opcode group: {opcodeGroupByte:X2} (instruction was {instruction:X2})");
            }

            return opcodeGroup.ExtractOpcodeBaseCodeFromInstruction(instruction);
        }

        private const byte GROUP_MASK = 0xF0;
        private readonly Dictionary<OpcodeBaseCode, IOpcode> _opcodes;
        private readonly Dictionary<OpcodeGroupBaseCode, IOpcodeGroup> _opcodeGroupRegistry;
    }
}
