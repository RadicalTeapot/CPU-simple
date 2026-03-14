using Emulator.CpuStates;
using Emulator.IO;
using CPU;

namespace Emulator.Commands.GlobalCommands
{
    internal interface IGlobalCommand : ICommand
    {
        GlobalCommandResult Execute(GlobalCommandExecutionContext executionContext, string[] args);
    }

    internal record GlobalCommandExecutionContext(
        CpuInspector Inspector,
        ICpuState CurrentState,
        BreakpointContainer Breakpoints,
        WatchpointContainer Watchpoints,
        IOutput Output) { }

    internal record GlobalCommandResult(
        bool Success,
        string? Message = null
    );
}
