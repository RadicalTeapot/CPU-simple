using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "tick", ["t"],
        description: "Tick the CPU for the specified number of micro-ticks.",
        helpText: "Usage: 'tick [count]'")]
    [ValidInState([typeof(IdleState), typeof(RunningState)])]
    internal class Tick(CommandContext context) : BaseStateCommand(context)
    {
        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            int tickCount;
            try { tickCount = ParseArgs(args); }
            catch (Exception e)
            {
                return new StateCommandResult(Success: false, Message: e.Message);
            }

            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateTickingState(tickCount),
                Message: $"Cpu will tick {tickCount} micro-ticks"
            );
        }

        private int ParseArgs(string[] args)
        {
            if (args.Length == 0) return 1;
            else if (int.TryParse(args[0], out var tickCount) && tickCount > 0) return tickCount;
            else throw new CommandException($"Invalid arguments for '{Name}' command.");
        }
    }
}
