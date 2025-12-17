using CPU.components;

namespace CPU.opcodes
{
    public interface IOpcode
    {
        byte Execute(out Trace trace);
    }

    internal abstract class BaseOpcode : IOpcode
    {
        internal enum RegisterArgsCount
        {
            One,
            Two,
        }

        public BaseOpcode(byte instructionSizeInByte, State cpuState, Memory memory, RegisterArgsCount registerArgsCount)
        {
            _instructionSizeInByte = instructionSizeInByte;
            CpuState = cpuState;
            Memory = memory;
            _registerParser = registerArgsCount == RegisterArgsCount.One
                ? new SingleRegisterInstructionParser()
                : new DoubleRegisterInstructionParser();
        }

        public byte Execute(out Trace trace)
        {
            var args = GetInstructionArgs();
            trace = Execute(args);
            return _instructionSizeInByte;
        }

        protected readonly State CpuState;
        protected readonly Memory Memory;

        protected abstract Trace Execute(byte[] args);

        private byte[] GetInstructionArgs()
        {
            var instruction = Memory.ReadByte(CpuState.PC);
            var args = new List<byte>();
            args.AddRange(_registerParser.ParseArguments(instruction));
            for (int i = 1; i < _instructionSizeInByte; i++)
            {
                args.Add(Memory.ReadByte(CpuState.PC + i));
            }
            return [.. args];
        }

        private interface RegisterParser
        {
            byte[] ParseArguments(byte instruction);
        }

        private class SingleRegisterInstructionParser : RegisterParser
        {
            public byte[] ParseArguments(byte instruction)
                => [(byte)(instruction & REGISTER_MASK)];

            private const byte REGISTER_MASK = 0x03;
        }

        private class DoubleRegisterInstructionParser : RegisterParser
        {
            public byte[] ParseArguments(byte instruction)
                => [(byte)((instruction >> 2) & REGISTER_MASK), (byte)(instruction & REGISTER_MASK)];

            private const byte REGISTER_MASK = 0x03;
        }

        private readonly byte _instructionSizeInByte;
        private readonly RegisterParser _registerParser;
    }

    internal class NOP : IOpcode
    {
        public byte Execute(out Trace trace)
        {
            // No operation
            trace = new Trace
            {
                InstructionName = nameof(NOP),
                Args = "-",
            };
            return 1;
        }
    }

    internal class HLT : IOpcode
    {
        public byte Execute(out Trace trace)
        {
            trace = new Trace
            {
                InstructionName = nameof(HLT),
                Args = "-",
            };
            throw new OpcodeException.HaltException();
        }
    }

    internal class MOV(State cpuState, Memory memory) : BaseOpcode(1, cpuState, memory, RegisterArgsCount.Two)
    {
        protected override Trace Execute(byte[] args)
        {
            var srcReg = args[0];
            var destReg = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(MOV),
                Args = $"RS: {srcReg}, RD: {destReg}",
                RBefore = [CpuState.GetRegister(destReg), CpuState.GetRegister(srcReg)],
            };

            var value = CpuState.GetRegister(srcReg);
            CpuState.SetRegister(destReg, value);

            trace.RAfter = [CpuState.GetRegister(destReg), CpuState.GetRegister(srcReg)];
            return trace;
        }
    }

    internal class LDI(State cpuState, Memory memory) : BaseOpcode(2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = args[0];
            var immediateValue = args[1];
            
            var trace = new Trace()
            {
                InstructionName = nameof(LDI),
                Args = $"RD: {destReg}, IMM: {immediateValue}",
                RBefore = [CpuState.GetRegister(destReg)],
            };

            CpuState.SetRegister(destReg, immediateValue);
            trace.RAfter = [CpuState.GetRegister(destReg)];
            return trace;
        }
    }

    internal class LDR(State cpuState, Memory memory) : BaseOpcode(2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = args[0];
            var memoryAddress = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(LDR),
                Args = $"RD: {destReg}, ADDR: {memoryAddress}",
                RBefore = [CpuState.GetRegister(destReg)],
            };

            CpuState.SetRegister(destReg, Memory.ReadByte(memoryAddress));
            trace.RAfter = [CpuState.GetRegister(destReg)];
            return trace;
        }
    }

    internal class STR(State cpuState, Memory memory) : BaseOpcode(2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var srcReg = args[0];
            var memoryAddress = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(STR),
                Args = $"RS: {srcReg}, Memory address: {memoryAddress}",
                RBefore = [CpuState.GetRegister(srcReg)],
            };

            Memory.WriteByte(memoryAddress, CpuState.GetRegister(srcReg));
            trace.RAfter = [CpuState.GetRegister(srcReg)];
            return trace;
        }
    }

}
