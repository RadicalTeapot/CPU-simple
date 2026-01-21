using Backend.CpuStates;

namespace Backend.Commands.StateCommands
{
    [Command(CommandType.State, "load", ["l"],
        description: "Loads a program into the CPU from a specified file path.",
        helpText: "Usage: 'load [path]'")]
    [ValidInState([typeof(IdleState), typeof(HaltedState), typeof(ErrorState)])]
    internal class Load(CommandContext context) : BaseStateCommand(context)
    {
        protected override StateCommandResult ExecuteCore(CpuStateFactory stateFactory, string[] args)
        {
            if (args.Length != 1)
            {
                return new StateCommandResult(Success: false, Message: $"Error: '{Name}' command requires exactly one argument: the file path.");
            }

            var programPath = args[0];
            byte[] programBytes;
            try
            {
                programBytes = File.ReadAllBytes(programPath);
            }
            catch (Exception ex)
            {
                return new StateCommandResult(Success: false, Message: $"Failed to read file '{programPath}': {ex.Message}");
            }
            
            //context.Cpu.LoadProgram(programBytes);
            return new StateCommandResult(
                Success: true,
                NextState: stateFactory.CreateLoadingState(programBytes),
                Message: $"Cpu will load program at '{programPath}'.");
        }
    }
}
