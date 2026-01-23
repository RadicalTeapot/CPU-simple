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

        public GlobalCommandResult Execute(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args)
        {
            if (ShouldPrintHelp(args))
            {
                return new GlobalCommandResult(
                    Success: true,
                    Message: HelpText ?? $"Usage: '{Name}'\n{Description}"
                );
            }
            return ExecuteCore(inspector, currentState, output, args);
        }

        protected abstract GlobalCommandResult ExecuteCore(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args);

        private static bool ShouldPrintHelp(string[] args)
        {
            return args.Length == 1
                && (args[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                    args[0].Equals("?", StringComparison.OrdinalIgnoreCase));
        }
    }
}
