using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    internal abstract class BaseGlobalCommand(CommandContext context) : IGlobalCommand
    {
        public string Name { get; } = context.Name;
        public string Description { get; } = context.Description;
        public string HelpText { get; } = context.HelpText;

        public GlobalCommandResult Execute(ExecutionContext executionContext, string[] args)
        {
            if (ShouldPrintHelp(args))
            {
                return new GlobalCommandResult(
                    Success: true,
                    Message: HelpText ?? $"Usage: '{Name}'\n{Description}"
                );
            }
            return ExecuteCore(executionContext, args);
        }

        protected abstract GlobalCommandResult ExecuteCore(ExecutionContext executionContext, string[] args);

        private static bool ShouldPrintHelp(string[] args)
        {
            return args.Length == 1
                && (args[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                    args[0].Equals("?", StringComparison.OrdinalIgnoreCase));
        }
    }
}
