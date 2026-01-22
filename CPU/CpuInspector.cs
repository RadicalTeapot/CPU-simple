using CPU.components;
using CPU.opcodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPU
{
    public class CpuInspector()
    {
        public int Cycle { get; private set; } = 0;
        public int PC { get; private set; } = 0;
        public int SP { get; private set; } = 0;
        public byte[] Registers { get; private set; } = [];
        public bool ZeroFlag { get; private set; } = false;
        public bool CarryFlag { get; private set; } = false;
        public string[] LastInstruction { get; private set; } = [];
        public byte[] StackContents { get; private set; } = [];
        public byte[] MemoryContents { get; private set; } = [];

        internal class Builder()
        {
            private readonly CpuInspector _inspector = new();
            public Builder SetCycle(int cycle)
            {
                _inspector.Cycle = cycle;
                return this;
            }
            public Builder SetPC(int pc)
            {
                _inspector.PC = pc;
                return this;
            }
            public Builder SetSP(int sp)
            {
                _inspector.SP = sp;
                return this;
            }
            public Builder SetRegisters(byte[] registers)
            {
                _inspector.Registers = registers;
                return this;
            }
            public Builder SetZeroFlag(bool zeroFlag)
            {
                _inspector.ZeroFlag = zeroFlag;
                return this;
            }
            public Builder SetCarryFlag(bool carryFlag)
            {
                _inspector.CarryFlag = carryFlag;
                return this;
            }
            public Builder SetLastInstruction(string[] lastInstruction)
            {
                _inspector.LastInstruction = lastInstruction;
                return this;
            }
            public Builder SetStack(byte[] stack)
            {
                _inspector.StackContents = stack;
                return this;
            }
            public Builder SetMemory(byte[] memory)
            {
                _inspector.MemoryContents = memory;
                return this;
            }
            public CpuInspector Build()
            {
                return _inspector;
            }
        }

        public static CpuInspector Create(int cycle, State state, Stack stack, Memory memory, string[] lastInstruction)
        {
            var builder = new Builder()
                .SetCycle(cycle)
                .SetLastInstruction(lastInstruction);
            state.UpdateCpuInspectorBuilder(builder);
            stack.UpdateCpuInspectorBuilder(builder);
            memory.UpdateCpuInspectorBuilder(builder);
            return builder.Build();
        }
    }
}
