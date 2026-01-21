using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    internal abstract class BaseStateCommand(CommandContext context) : IStateCommand
    {
        public string Name { get; } = context.Name;
        public string Description { get; } = context.Description;
        public string HelpText { get; } = context.HelpText;

        public StateCommandResult GetStateForCommand(CpuStateFactory stateFactory, string[] args)
        {
            if (ShouldPrintHelp(args))
            {
                return new StateCommandResult(
                    Success: true,
                    Message: HelpText
                );
            }
            return ExecuteCore(stateFactory, args);
        }

        protected abstract StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args);

        private static bool ShouldPrintHelp(string[] args)
        {
            return args.Length == 1
                && (args[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                    args[0].Equals("?", StringComparison.OrdinalIgnoreCase));
        }
    }
}
