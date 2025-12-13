using CPU.components;

namespace CPU.opcodes
{
    public interface IOpcode
    {
        byte Execute(out Trace trace);
    }

    internal abstract class BaseOpcode : IOpcode
    {
        public BaseOpcode(byte instructionSize, State cpuState, Memory memory)
        {
            _instructionSize = instructionSize;
            CpuState = cpuState;
            Memory = memory;
        }

        public byte Execute(out Trace trace)
        {
            var args = GetInstructionArgs();
            trace = Execute(args);
            return _instructionSize;
        }

        protected readonly State CpuState;
        protected readonly Memory Memory;

        protected abstract Trace Execute(byte[] args);

        private byte[] GetInstructionArgs()
        {
            var instruction = Memory.ReadByte(CpuState.PC);
            var args = new byte[_instructionSize];
            args[0] = (byte)(instruction & 0x0F);
            for (int i = 1; i < _instructionSize; i++)
            {
                args[i] = Memory.ReadByte(CpuState.PC + i);
            }
            return args;
        }

        private readonly byte _instructionSize;
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

    internal class MOV(State cpuState, Memory memory) : BaseOpcode(1, cpuState, memory)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = (byte)((args[0] >> 2) & 0x03);
            var srcReg = (byte)(args[0] & 0x03);

            var trace = new Trace()
            {
                InstructionName = nameof(MOV),
                Args = $"RD: {destReg}, RS: {srcReg}",
                RBefore = [CpuState.GetRegister(destReg), CpuState.GetRegister(srcReg)],
            };

            var value = CpuState.GetRegister(srcReg);
            CpuState.SetRegister(destReg, value);

            trace.RAfter = [CpuState.GetRegister(destReg), CpuState.GetRegister(srcReg)];
            return trace;
        }
    }

    internal class LDI(State cpuState, Memory memory) : BaseOpcode(2, cpuState, memory)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = (byte)((args[0] >> 2) & 0x03);
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

    internal class LDR(State cpuState, Memory memory) : BaseOpcode(2, cpuState, memory)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = (byte)((args[0] >> 2) & 0x03);
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

    internal class STR(State cpuState, Memory memory) : BaseOpcode(2, cpuState, memory)
    {
        protected override Trace Execute(byte[] args)
        {
            var srcReg = (byte)((args[0] >> 2) & 0x03);
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
