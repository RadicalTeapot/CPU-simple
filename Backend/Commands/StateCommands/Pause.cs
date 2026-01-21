using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "pause", ["stop", "p"],
        description: "Pauses the CPU execution.", helpText: "Usage: 'pause'")]
    [ValidInState([typeof(RunningState), typeof(SteppingState)])]
    internal class Pause(CommandContext context) : BaseStateCommand(context)
    {
        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            if (args.Length != 0)
            {
                return new StateCommandResult(Success: false, Message: $"'{Name}' takes no arguments.");
            }

            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateIdleState(),
                Message: "CPU execution will be paused."
            );
        }
    }
}
