using CPU.components;
using CPU.microcode;

namespace CPU
{
    public class CpuInspector
    {
        public int Cycle { get; }
        public int PC { get; }
        public int SP { get; }
        public byte[] Registers { get; }
        public bool ZeroFlag { get; }
        public bool CarryFlag { get; }
        public byte[] StackContents { get; }
        public byte[] MemoryContents { get; }
        public TickTrace[] Traces { get; }
        public bool ProgramLoaded { get; }

        internal CpuInspector(int cycle, State state, Stack stack, Memory memory, bool programLoaded, TickTracer tracer)
        {
            Cycle = cycle;
            ProgramLoaded = programLoaded;
            PC = state.GetPC();
            ZeroFlag = state.Z;
            CarryFlag = state.C;
            var registers = new byte[state.RegisterCount];
            for (int i = 0; i < state.RegisterCount; i++)
            {
                registers[i] = state.GetRegister(i);
            }
            Registers = registers;
            SP = stack.SP;
            var stackContents = new byte[stack.Size];
            for (int i = 0; i < stack.Size; i++)
            {
                stackContents[i] = stack.PeekByte(i);
            }
            StackContents = stackContents;
            var memoryContents = new byte[memory.Size];
            for (int i = 0; i < memory.Size; i++)
            {
                memoryContents[i] = memory.ReadByte(i);
            }
            MemoryContents = memoryContents;
            Traces = tracer.LastTraces;
        }
    }
}
