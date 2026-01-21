using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "step", ["s"],
        description: "Step the CPU for a the specified number of instructions.",
        helpText: "Usage: 'step [count]'")]
    [ValidInState([typeof(IdleState), typeof(RunningState)])]
    internal class Step(CommandContext context) : BaseStateCommand(context)
    {
        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            int stepCount;
            try
            {
                stepCount = ParseArgs(args);
            }
            catch (Exception e) 
            {
                return new StateCommandResult(
                    Success: false,
                    Message: e.Message);
            }

            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateSteppingState(stepCount),
                Message: $"Cpu will step {stepCount} instructions"
            );
        }

        private int ParseArgs(string[] args)
        {
            if (args.Length == 0)
            {
                return 1;
            }
            else if (int.TryParse(args[0], out var stepCount) && stepCount > 0)
            {
                return stepCount;
            }
            else
            {
                throw new CommandException($"Invalid arguments for '{Name}' command.");
            }
        }
    }
}
