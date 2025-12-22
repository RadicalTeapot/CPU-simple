using CPU.components;
using System.Diagnostics;

namespace CPU.opcodes
{
    /// <summary>
    /// Factory class responsible for creating and managing opcodes.
    /// </summary>
    /// <remarks>This plays the role of the "Control unit" in a CPU architecture.</remarks>
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
            new AddOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);
            new SubOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);
            new BitsManipulationOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);
            new TwoRegistersCompareOpcodeGroup().RegisterGroup(_opcodeGroupRegistry);

            _opcodes = [];
            // Register opcodes
            new NOP(cpuState).RegisterOpcode(_opcodes);
            new HLT().RegisterOpcode(_opcodes);
            new CLC(cpuState).RegisterOpcode(_opcodes);
            new SEC(cpuState).RegisterOpcode(_opcodes);
            new CLZ(cpuState).RegisterOpcode(_opcodes);
            new SEZ(cpuState).RegisterOpcode(_opcodes);
            new JMP(cpuState, memory).RegisterOpcode(_opcodes);
            new JCC(cpuState, memory).RegisterOpcode(_opcodes);
            new JCS(cpuState, memory).RegisterOpcode(_opcodes);
            new JZC(cpuState, memory).RegisterOpcode(_opcodes);
            new JZS(cpuState, memory).RegisterOpcode(_opcodes);
            new CAL(cpuState, memory, stack).RegisterOpcode(_opcodes);
            new RET(cpuState, stack).RegisterOpcode(_opcodes);
            new LDI(cpuState, memory).RegisterOpcode(_opcodes);
            new LDA(cpuState, memory).RegisterOpcode(_opcodes);
            new POP(cpuState, memory, stack).RegisterOpcode(_opcodes);
            new PEK(cpuState, memory, stack).RegisterOpcode(_opcodes);
            new STA(cpuState, memory).RegisterOpcode(_opcodes);
            new PSH(cpuState, memory, stack).RegisterOpcode(_opcodes);
            new MOV(cpuState, memory).RegisterOpcode(_opcodes);
            new ADI(cpuState, memory).RegisterOpcode(_opcodes);
            new ADA(cpuState, memory).RegisterOpcode(_opcodes);
            new SBI(cpuState, memory).RegisterOpcode(_opcodes);
            new SBA(cpuState, memory).RegisterOpcode(_opcodes);
            new ADD(cpuState, memory).RegisterOpcode(_opcodes);
            new SUB(cpuState, memory).RegisterOpcode(_opcodes);
            new LSH(cpuState, memory).RegisterOpcode(_opcodes);
            new RSH(cpuState, memory).RegisterOpcode(_opcodes);
            new LRT(cpuState, memory).RegisterOpcode(_opcodes);
            new RRT(cpuState, memory).RegisterOpcode(_opcodes);
            new CMP(cpuState, memory).RegisterOpcode(_opcodes);
        }

        public IOpcode GetOpcodeFromInstruction(byte instruction)
        {
            var opcodeGroup = GetOpcodeGroupFromInstruction(instruction);
            var opcodeBaseCode = opcodeGroup.ExtractOpcodeBaseCodeFromInstruction(instruction);

            Debug.Assert(
                _opcodes.ContainsKey(opcodeBaseCode),
                $"Unregistered opcode base code: {opcodeBaseCode} (instruction was {instruction:X2})");

            return _opcodes[opcodeBaseCode];
        }

        private IOpcodeGroup GetOpcodeGroupFromInstruction(byte instruction)
        {
            var opcodeGroupByte = (byte)(instruction & GROUP_MASK);

            Debug.Assert(
                Enum.IsDefined(typeof(OpcodeGroupBaseCode), opcodeGroupByte), 
                $"Unknown opcode group byte: {opcodeGroupByte:X2} (instruction was {instruction:X2})");
            Debug.Assert(
                _opcodeGroupRegistry.ContainsKey((OpcodeGroupBaseCode)opcodeGroupByte),
                $"Opcode group not registered: {opcodeGroupByte:X2} (instruction was {instruction:X2})");

            return _opcodeGroupRegistry[(OpcodeGroupBaseCode)opcodeGroupByte];
        }

        private const byte GROUP_MASK = 0xF0;
        private readonly Dictionary<OpcodeBaseCode, IOpcode> _opcodes;
        private readonly Dictionary<OpcodeGroupBaseCode, IOpcodeGroup> _opcodeGroupRegistry;
    }
}
