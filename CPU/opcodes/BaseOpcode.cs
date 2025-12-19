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
        public byte FirstRegisterId = 0;    // Used only if applicable, bits 0-1
        public byte SecondRegisterId = 0;   // Used only if applicable, bits 2-3
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

        protected abstract Trace Execute(OpcodeArgs args);

        private OpcodeArgs ParseArguments()
        {
            var instruction = Memory.ReadByte(CpuState.GetPC());
            CpuState.IncrementPC();

            var args = new OpcodeArgs();
            _registerParser.ParseArguments(instruction, ref args);
            switch (operandType)
            {
                case OperandType.None:
                    break;
                case OperandType.Address:
                    args.AddressValue = Memory.ReadAddress(CpuState.GetPC(), out var size);
                    CpuState.IncrementPC(size);
                    break;
                case OperandType.Immediate:
                    args.ImmediateValue = Memory.ReadByte(CpuState.GetPC());
                    CpuState.IncrementPC();
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
                args.FirstRegisterId = (byte)(instruction & REGISTER_MASK);
            }

            private const byte REGISTER_MASK = 0x03;
        }

        private class DoubleRegisterInstructionParser : IRegisterParser
        {
            public void ParseArguments(byte instruction, ref OpcodeArgs args)
            {
                args.FirstRegisterId = (byte)(instruction & REGISTER_MASK);
                args.SecondRegisterId = (byte)((instruction >> 2) & REGISTER_MASK);
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