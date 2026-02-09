using Backend.Commands.GlobalCommands;
using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Tests
{
    internal class TestLogger : ILogger
    {
        public List<string> LogMessages { get; } = [];
        public List<string> ErrorMessages { get; } = [];
        public bool UsageLogged { get; private set; }

        public void Log(string message) => LogMessages.Add(message);
        public void Error(string message) => ErrorMessages.Add(message);
        public void LogUsage() => UsageLogged = true;
    }

    internal class TestOutput : IOutput
    {
        public List<CpuInspector> StatusWrites { get; } = [];
        public List<byte[]> MemoryDumps { get; } = [];
        public List<byte[]> StackDumps { get; } = [];
        public List<int[]> BreakpointLists { get; } = [];
        public List<int> BreakpointHits { get; } = [];

        public void WriteStatus(CpuInspector inspector) => StatusWrites.Add(inspector);
        public void WriteMemoryDump(byte[] memoryDump) => MemoryDumps.Add(memoryDump);
        public void WriteStackDump(byte[] stackDump) => StackDumps.Add(stackDump);
        public void WriteBreakpointList(int[] breakpoints) => BreakpointLists.Add(breakpoints);
        public void WriteBreakpointHit(int address) => BreakpointHits.Add(address);
    }

    internal static class BackendTestHelpers
    {
        public static GlobalCommandExecutionContext CreateGlobalContext(
            int memorySize = 256, int stackSize = 16, int registerCount = 4)
        {
            var config = new Config(memorySize, stackSize, registerCount);
            var cpu = new CPU.CPU(config);
            var inspector = cpu.GetInspector();
            var breakpoints = new BreakpointContainer();
            var output = new TestOutput();
            return new GlobalCommandExecutionContext(inspector, new FakeIdleState(), breakpoints, output);
        }

        public static GlobalCommandExecutionContext CreateGlobalContextWithProgram(
            byte[] program, int memorySize = 256, int stackSize = 16, int registerCount = 4)
        {
            var config = new Config(memorySize, stackSize, registerCount);
            var cpu = new CPU.CPU(config);
            cpu.LoadProgram(program);
            var inspector = cpu.GetInspector();
            var breakpoints = new BreakpointContainer();
            var output = new TestOutput();
            return new GlobalCommandExecutionContext(inspector, new FakeIdleState(), breakpoints, output);
        }
    }

    internal class FakeIdleState : ICpuState
    {
        public ICpuState GetStateForCommand(Commands.StateCommands.IStateCommand command, string[] args) => this;
        public ICpuState Tick() => this;
        public void LogHelp() { }
    }
}
