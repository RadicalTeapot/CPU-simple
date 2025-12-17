using CPU.components;

namespace CPU.opcodes
{
    public enum OpcodeBaseCode : byte
    {
        NOP = 0x00,
        HLT = 0x01,
        LDI = 0x10,
        LDR = 0x14,
        STR = 0x24,
        MOV = 0x30,
        ADI = 0x40,
    }

    public interface IOpcode
    {
        void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry);
        byte Execute(out Trace trace);
    }

    internal abstract class BaseOpcode(OpcodeBaseCode opcodeBaseCode, byte instructionSizeInByte, State cpuState, Memory memory, BaseOpcode.RegisterArgsCount registerArgsCount) : IOpcode
    {
        internal enum RegisterArgsCount
        {
            One,
            Two,
        }

        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[opcodeBaseCode] = this;

        public byte Execute(out Trace trace)
        {
            var args = ParseArguments();
            trace = Execute(args);
            return instructionSizeInByte;
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

        private readonly IRegisterParser _registerParser = registerArgsCount == RegisterArgsCount.One
                ? new SingleRegisterInstructionParser()
                : new DoubleRegisterInstructionParser();
    }

    internal class NOP : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.NOP] = this;

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
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.HLT] = this;

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

    internal class MOV(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.MOV, 1, cpuState, memory, RegisterArgsCount.Two)
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

    internal class LDI(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.LDI, 2, cpuState, memory, RegisterArgsCount.One)
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

    internal class LDR(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.LDR, 2, cpuState, memory, RegisterArgsCount.One)
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

    internal class STR(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.STR, 2, cpuState, memory, RegisterArgsCount.One)
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

    internal class ADI(State cpuState, Memory memory): BaseOpcode(OpcodeBaseCode.ADI, 2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = args[0];
            var immediateValue = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(ADI),
                Args = $"RD: {destReg}, IMM: {immediateValue}",
                RBefore = [CpuState.GetRegister(destReg)],
            };

            var currentValue = CpuState.GetRegister(destReg);
            var result = currentValue + immediateValue;
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetRegister(destReg, (byte)result);
            CpuState.SetZeroFlag(result == 0);
            trace.RAfter = [CpuState.GetRegister(destReg)];
            return trace;
        }
    }
}