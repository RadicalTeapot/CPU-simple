using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "run", ["r"],
        description: "Runs the CPU until the specified stop condition.",
        helpText: "Usage: 'run' to run until halt, 'run to_address <address>' to run until the specified address is reached.")]
    [ValidInState([typeof(IdleState), typeof(SteppingState)])]
    internal class Run(CommandContext context) : BaseStateCommand(context)
    {
        public enum Mode
        {
            ToHalt,
            ToAddress,
            //ToBreakpoint,
            //ToSymbol,
        }

        public record Config(Mode Mode, int Address);

        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            Config config;
            try
            {
                config = ParseArgs(args);
            }
            catch (CommandException ex)
            {
                return new StateCommandResult(
                    Success: false,
                    Message: ex.Message
                );
            }

            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateRunningState(config),
                Message: "CPU has started running."
            );
        }

        private Config ParseArgs(string[] args)
        {
            if (args.Length == 0)
            {
                return new Config(Mode.ToHalt, 0);
            }
            else if (args[0] == "to_address")
            {
                if (args.Length != 2)
                {
                    throw new CommandException("The 'run to_address' command requires exactly one argument: the address.");
                }

                if (!int.TryParse(args[1], out var address) || address < 0)
                {
                    throw new CommandException("The address must be a non-negative integer.");
                }

                return new Config(Mode.ToAddress, address);
            }
            else
            {
                throw new CommandException($"Invalid arguments for '{Name}' command.");
            }
        }
    }
}
