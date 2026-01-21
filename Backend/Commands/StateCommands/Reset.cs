using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "reset", description: "Resets the CPU.", helpText: " Usage: 'reset'")]
    [ValidInState([typeof(IdleState), typeof(HaltedState)])]
    internal class Reset(CommandContext context) : BaseStateCommand(context)
    {
        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            if (args.Length != 0)
            {
                return new StateCommandResult(Success: false, Message: $"'{Name}' command takes no arguments.");
            }
            
            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateResetState(),
                Message: "CPU will be reset.");
        }
    }
}
