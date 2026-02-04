using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    internal interface IStateCommand : ICommand
    {
        StateCommandResult Execute(CpuStateFactory stateFactory, string[] args);
    }

    internal record StateCommandResult(
        bool Success,
        ICpuState? NextState = null, // Null if no CPU state change
        string? Message = null
    );
}
