using Emulator.Commands.StateCommands;

namespace Emulator.CpuStates
{
    internal interface ICpuState
    {
        ICpuState GetStateForCommand(IStateCommand command, string[] args);

        ICpuState Tick();

        void LogHelp();
    }
}
