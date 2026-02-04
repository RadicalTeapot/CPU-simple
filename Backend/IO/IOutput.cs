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
                type = "status",
                cycle = inspector.Cycle,
                pc = inspector.PC,
                sp = inspector.SP,
                registers = inspector.Registers.Select(v => (int)v).ToArray(), // Convert bytes to ints for JSON serialization
                zero_flag = inspector.ZeroFlag,
                carry_flag = inspector.CarryFlag,
                memory_changes = inspector.MemoryChanges,
                stack_changes = inspector.StackChanges,
                program_loaded = inspector.ProgramLoaded
            });
        }

        public void WriteMemoryDump(byte[] memoryDump)
        {
            OutputData(new
            {
                type = "memory_dump",
                memory = memoryDump.Select(v => (int)v).ToArray() // Convert bytes to ints for JSON serialization
            });
        }

        public void WriteStackDump(byte[] stackDump)
        {
            OutputData(new
            {
                type = "stack_dump",
                stack = stackDump.Select(v => (int)v).ToArray() // Convert bytes to ints for JSON serialization
            });
        }

        public void WriteBreakpointList(int[] breakpoints)
        {
            OutputData(new
            {
                type = "breakpoint_list",
                breakpoints = breakpoints
            });
        }

        public void WriteBreakpointHit(int address)
        {
            OutputData(new
            {
                type = "breakpoint_hit",
                address = address
            });
        }

        private static void OutputData(object jsonObject)
        {
            var jsonString = JsonSerializer.Serialize(jsonObject);
            Console.Out.WriteLine(jsonString);
        }
    }
}
