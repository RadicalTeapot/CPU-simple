namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "breakpoint", ["bp"],
        description: "Toggle or remove brakpoint(s)",
        helpText: "Usage: 'breakpoint [toggle/clear/list] [address]'")]
    internal class Breakpoint(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command either one or two arguments.");
            }
            var action = args[0].ToLower();

            string resultMessage;
            switch (action)
            {
                case "toggle":
                    if (!int.TryParse(args[1], out int address) || address < 0 || address > 0xFFFF)
                    {
                        return new GlobalCommandResult(Success: false, Message: $"The address '{args[1]}' is not a valid memory address.");
                    }

                    if (executionContext.Breakpoints.Contains(address))
                    {
                        executionContext.Breakpoints.Remove(address);
                        resultMessage = $"Breakpoint removed at address 0x{address:X4}.";
                    }
                    else
                    {
                        executionContext.Breakpoints.Add(address);
                        resultMessage = $"Breakpoint added at address 0x{address:X4}.";
                    }
                    break;
                case "clear":
                    executionContext.Breakpoints.Clear();
                    resultMessage = $"All breakpoints have been removed.";
                    break;
                case "list":
                    if (executionContext.Breakpoints.Count == 0)
                    {
                        resultMessage = "No breakpoints set.";
                    }
                    else
                    {
                        var breakpointAddresses = executionContext.Breakpoints.GetAll().Select(bp => $"0x{bp.Address:X4}");
                        resultMessage = $"Current breakpoints at addresses: {string.Join(", ", breakpointAddresses)}";
                    }
                    break;
                default:
                    return new GlobalCommandResult(Success: false, Message: $"The action '{action}' is not valid for the '{Name}' command. Use 'toggle' or 'remove'.");
            }

            var breakpoints = executionContext.Breakpoints.GetAll();
            var outputBreakpointList = string.Join(" ", breakpoints.Select(bp => bp.Address));
            executionContext.Output.Write($"[BP] {outputBreakpointList}");

            return new GlobalCommandResult(Success: true, Message: resultMessage);
        }
    }
}
