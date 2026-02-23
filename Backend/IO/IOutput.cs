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
                traces = inspector.Traces.Select(t => new
                {
                    tick = t.TickNumber,
                    tick_type = t.Type.ToString(),
                    phase = t.Phase.ToString(),
                    pc_before = t.PcBefore,
                    pc_after = t.PcAfter,
                    sp_before = t.SpBefore,
                    sp_after = t.SpAfter,
                    instruction = t.Instruction,
                    register_changes = t.RegisterChanges.Select(rc => new
                    {
                        index = rc.Index,
                        old_value = (int)rc.OldValue,
                        new_value = (int)rc.NewValue,
                    }).ToArray(),
                    zero_flag_before = t.ZeroFlagBefore,
                    zero_flag_after = t.ZeroFlagAfter,
                    carry_flag_before = t.CarryFlagBefore,
                    carry_flag_after = t.CarryFlagAfter,
                    bus = t.Bus == null ? null : new
                    {
                        address = t.Bus.Address,
                        data = (int)t.Bus.Data,
                        direction = t.Bus.Direction.ToString(),
                        type = t.Bus.Type.ToString(),
                    },
                }).ToArray(),
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
