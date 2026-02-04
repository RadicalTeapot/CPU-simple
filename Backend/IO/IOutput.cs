using System.Text.Json;

namespace Backend.IO
{
    public interface IOutput
    {
        void WriteStatus(CPU.CpuInspector inspector);
        void WriteMemoryDump(byte[] memoryDump);
        void WriteStackDump(byte[] stackDump);
        void WriteBreakpointList(int[] breakpoints);
        void WriteBreakpointHit(int address);
    }

    // Output is done on STDOUT
    internal class ConsoleOutput : IOutput
    {
        public void WriteStatus(CPU.CpuInspector inspector)
        {
            OutputData(new
            {
                Type = "status",
                Cycle = inspector.Cycle,
                PC = inspector.PC,
                SP = inspector.SP,
                Registers = inspector.Registers.Select(v => (int)v).ToArray(), // Convert bytes to ints for JSON serialization
                ZeroFlag = inspector.ZeroFlag,
                CarryFlag = inspector.CarryFlag,
                MemoryChanges = inspector.MemoryChanges,
                StackChanges = inspector.StackChanges,
                ProgramLoaded = inspector.ProgramLoaded
            });
        }

        public void WriteMemoryDump(byte[] memoryDump)
        {
            OutputData(new
            {
                Type = "memory_dump",
                Memory = memoryDump.Select(v => (int)v).ToArray() // Convert bytes to ints for JSON serialization
            });
        }

        public void WriteStackDump(byte[] stackDump)
        {
            OutputData(new
            {
                Type = "stack_dump",
                Stack = stackDump.Select(v => (int)v).ToArray() // Convert bytes to ints for JSON serialization
            });
        }

        public void WriteBreakpointList(int[] breakpoints)
        {
            OutputData(new
            {
                Type = "breakpoint_list",
                Breakpoints = breakpoints
            });
        }

        public void WriteBreakpointHit(int address)
        {
            OutputData(new
            {
                Type = "breakpoint_hit",
                Address = address
            });
        }

        private static void OutputData(object jsonObject)
        {
            var jsonString = JsonSerializer.Serialize(jsonObject);
            Console.Out.WriteLine(jsonString);
        }
    }
}
