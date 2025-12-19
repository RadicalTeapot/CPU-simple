using CPU.components;

namespace CPU.opcodes
{
    internal abstract class BaseOpcode(OpcodeBaseCode opcodeBaseCode, byte instructionSizeInByte, State cpuState, Memory memory, BaseOpcode.RegisterArgsCount registerArgsCount) : IOpcode
    {
        internal enum RegisterArgsCount
        {
            Zero,
            One,
            Two,
        }

        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[opcodeBaseCode] = this;

        public void Execute(out Trace trace)
        {
            var args = ParseArguments();
            trace = Execute(args);

            trace.PcBefore = CpuState.PC;
            trace.PcAfter = (byte)(CpuState.PC + instructionSizeInByte);
            
            CpuState.IncrementPC(instructionSizeInByte);
        }

        protected readonly State CpuState = cpuState;
        protected readonly Memory Memory = memory;

        protected abstract Trace Execute(byte[] args);

        private byte[] ParseArguments()
        {
            var instruction = Memory.ReadByte(CpuState.PC);
            var args = new List<byte>();
            args.AddRange(_registerParser.ParseArguments(instruction));
            for (int i = 1; i < instructionSizeInByte; i++)
            {
                args.Add(Memory.ReadByte(CpuState.PC + i));
            }
            return [.. args];
        }

        private interface IRegisterParser
        {
            byte[] ParseArguments(byte instruction);
        }

        private class NoRegisterInstructionParser : IRegisterParser
        {
            public byte[] ParseArguments(byte instruction)
                => [];
        }

        private class SingleRegisterInstructionParser : IRegisterParser
        {
            public byte[] ParseArguments(byte instruction)
                => [(byte)(instruction & REGISTER_MASK)];

            private const byte REGISTER_MASK = 0x03;
        }

        private class DoubleRegisterInstructionParser : IRegisterParser
        {
            public byte[] ParseArguments(byte instruction)
                => [(byte)((instruction >> 2) & REGISTER_MASK), (byte)(instruction & REGISTER_MASK)];

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