using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    internal interface IGlobalCommand : ICommand
    {
        GlobalCommandResult Execute(ExecutionContext executionContext, string[] args);
    }
    internal record ExecutionContext(
        CpuInspector Inspector, ICpuState CurrentState, IOutput Output) { }


    internal record GlobalCommandResult(
        bool Success,
        string? Message = null
    );
}
