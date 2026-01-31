using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    internal interface IGlobalCommand : ICommand
    {
        GlobalCommandResult Execute(GlobalCommandExecutionContext executionContext, string[] args);
    }

    internal record GlobalCommandExecutionContext(
        CpuInspector Inspector, 
        ICpuState CurrentState, 
        BreakpointContainer Breakpoints, 
        IOutput Output) { }

    internal record GlobalCommandResult(
        bool Success,
        string? Message = null
    );
}
