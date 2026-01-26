using CPU.components;

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
        public KeyValuePair<int, byte>[] MemoryChanges { get; private set; } = [];
        public KeyValuePair<int, byte>[] StackChanges { get; private set; } = [];

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
            public Builder SetMemoryChanges(KeyValuePair<int, byte>[] changes)
            {
                _inspector.MemoryChanges = [..changes];
                return this;
            }
            public Builder SetStackChanges(KeyValuePair<int, byte>[] changes)
            {
                _inspector.StackChanges = [..changes];
                return this;
            }
            public CpuInspector Build()
            {
                return _inspector;
            }
        }

        internal static CpuInspector Create(int cycle, State state, Stack stack, Memory memory, ExecutionContext executionContext)
        {
            var builder = new Builder()
                .SetCycle(cycle)
                .SetLastInstruction(executionContext.LastInstruction)
                .SetMemoryChanges([..executionContext.MemoryChanges])
                .SetStackChanges([..executionContext.StackChanges]);
            state.UpdateCpuInspectorBuilder(builder);
            stack.UpdateCpuInspectorBuilder(builder);
            memory.UpdateCpuInspectorBuilder(builder);
            return builder.Build();
        }
    }
}
