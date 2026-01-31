namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "help", ["?"], "Displays help information.")]
    internal class Help(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length != 0)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command does not take any arguments.");
            }

            executionContext.CurrentState.LogHelp();
            return new GlobalCommandResult(Success: true);
        }
    }
}
