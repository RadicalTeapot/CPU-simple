using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    internal interface IGlobalCommand : ICommand
    {
        GlobalCommandResult Execute(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args);
    }

    internal record GlobalCommandResult(
        bool Success,
        string? Message = null
    );
}
