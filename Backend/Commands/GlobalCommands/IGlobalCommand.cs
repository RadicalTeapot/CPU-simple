using Backend.CpuStates;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    internal interface IGlobalCommand : ICommand
    {
        GlobalCommandResult Execute(CpuInspector inspector, ICpuState currentState, string[] args);
    }

    internal record GlobalCommandResult(
        bool Success,
        string? Message = null
    );
}
