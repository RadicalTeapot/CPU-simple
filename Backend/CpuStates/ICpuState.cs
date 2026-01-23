using Backend.Commands.StateCommands;

namespace Backend.CpuStates
{
    internal interface ICpuState
    {
        ICpuState GetStateForCommand(IStateCommand command, string[] args);

        ICpuState Tick();

        void LogHelp();
    }
}
