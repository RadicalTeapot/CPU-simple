using CPU.components;

namespace CPU.opcodes
{
    internal enum RegisterArgsCount
    {
        Zero,
        One,
        Two,
    }

    internal enum OperandType
    {
        None,
        Address,
        Immediate,
    }

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
        public byte ImmediateValue = 0;
#if x16
        public ushort AddressValue = 0;
#else
        public byte AddressValue = 0;
#endif
    }

    internal abstract class BaseOpcode(
        OpcodeBaseCode opcodeBaseCode, RegisterArgsCount registerArgsCount, OperandType operandType, 
        State cpuState, Memory memory) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[opcodeBaseCode] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = CpuState.GetPC();
            var args = ParseArguments();
            trace = Execute(args);

            trace.PcBefore = pcBefore;
            trace.PcAfter = CpuState.GetPC();
        }

        protected readonly State CpuState = cpuState;
        protected readonly Memory Memory = memory;

        /// <summary>
        /// Inner execution logic of the opcode.
        /// </summary>
        /// <param name="args">Parsed opcode arguments</param>
        /// <returns>Execution trace</returns>
        /// <remarks>PC has already been incremented before calling this</remarks>
        protected abstract Trace Execute(OpcodeArgs args);

        /// <summary>
        /// Reads and parses opcode arguments from memory, updating PC accordingly.
        /// </summary>
        /// <returns>Parsed opcode arguments</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="OperandType"/> is invalid</exception>
        private OpcodeArgs ParseArguments()
        {
            var args = new OpcodeArgs();

            var instruction = Memory.ReadByte(CpuState.GetPC());
            _registerParser.ParseArguments(instruction, ref args);
            CpuState.IncrementPC(); // Move past instruction byte (to operand, if any)

            switch (operandType)
            {
                case OperandType.None:
                    break;
                case OperandType.Address:
                    args.AddressValue = Memory.ReadAddress(CpuState.GetPC(), out var size);
                    CpuState.IncrementPC(size); // Move past address operand
                    break;
                case OperandType.Immediate:
                    args.ImmediateValue = Memory.ReadByte(CpuState.GetPC());
                    CpuState.IncrementPC(); // Move past immediate operand
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return args;
        }

        private interface IRegisterParser
        {
            void ParseArguments(byte instruction, ref OpcodeArgs args);
        }

        private class NoRegisterInstructionParser : IRegisterParser
        {
            public void ParseArguments(byte instruction, ref OpcodeArgs args) { }
        }

        private class SingleRegisterInstructionParser : IRegisterParser
        {
            public void ParseArguments(byte instruction, ref OpcodeArgs args)
            {
                args.LowRegisterIdx = (byte)(instruction & REGISTER_MASK);
            }

            private const byte REGISTER_MASK = 0x03;
        }

        private class DoubleRegisterInstructionParser : IRegisterParser
        {
            public void ParseArguments(byte instruction, ref OpcodeArgs args)
            {
                args.HighRegisterIdx = (byte)((instruction >> 2) & REGISTER_MASK);  // Bits 2-3, typically source register
                args.LowRegisterIdx = (byte)(instruction & REGISTER_MASK);          // Bits 0-1, typically destination register
            }

            private const byte REGISTER_MASK = 0x03;
        }

        private readonly IRegisterParser _registerParser = registerArgsCount switch
        {
            RegisterArgsCount.Zero => new NoRegisterInstructionParser(),
            RegisterArgsCount.One => new SingleRegisterInstructionParser(),
            RegisterArgsCount.Two => new DoubleRegisterInstructionParser(),
            _ => throw new ArgumentOutOfRangeException(nameof(registerArgsCount), registerArgsCount, null)
        };
    }
}