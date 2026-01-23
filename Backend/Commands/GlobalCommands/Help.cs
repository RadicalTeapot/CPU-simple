using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "help", ["?"], "Displays help information.")]
    internal class Help(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args)
        {
            if (args.Length != 0)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command does not take any arguments.");
            }

            currentState.LogHelp();
            return new GlobalCommandResult(Success: true);
        }
    }
}
