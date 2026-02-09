namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "breakpoint", ["bp"],
        description: "Toggle or remove breakpoint(s)",
        helpText: "Usage: 'breakpoint [toggle/clear/list] [address]'")]
    internal class Breakpoint(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command takes one or two arguments.");
            }
            var action = args[0].ToLower();

            string resultMessage;
            switch (action)
            {
                case "toggle":
                    if (args.Length < 2)
                    {
                        return new GlobalCommandResult(Success: false, Message: "The 'toggle' action requires an address argument.");
                    }
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
                        var addresses = executionContext.Breakpoints.GetAll().Select(bp => $"0x{bp.Address:X4}");
                        resultMessage = $"Current breakpoints at addresses: {string.Join(", ", addresses)}";
                    }
                    break;
                default:
                    return new GlobalCommandResult(Success: false, Message: $"The action '{action}' is not valid for the '{Name}' command. Use 'toggle', 'clear', or 'list'.");
            }

            executionContext.Output.WriteBreakpointList([..executionContext.Breakpoints.GetAll().Select(bp => bp.Address)]);

            return new GlobalCommandResult(Success: true, Message: resultMessage);
        }
    }
}
