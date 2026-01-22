using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "help", ["?"], "Displays help information.", "Usage: help\nDisplays this help information.")]
    internal class Help(CommandContext context) : IGlobalCommand
    {
        public string Name { get => context.Name; }

        public string Description { get => context.Description; }

        public string HelpText { get => context.HelpText; }

        public GlobalCommandResult Execute(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args)
        {
            currentState.LogHelp();
            return new GlobalCommandResult(Success: true);
        }
    }
}
