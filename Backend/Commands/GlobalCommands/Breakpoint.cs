using Backend.IO;
using System.Net;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "breakpoint",
    description: "Toggle or remove brakpoint(s)",
    helpText: "Usage: 'breakpoint [toggle/clear/list] [address]'")]
    internal class Breakpoint(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length != 2)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command requires exactly 2 arguments.");
            }
            var action = args[0].ToLower();

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
                        return new GlobalCommandResult(Success: true, Message: $"Breakpoint removed at address 0x{address:X4}.");
                    }
                    else
                    {
                        executionContext.Breakpoints.Add(address);
                        return new GlobalCommandResult(Success: true, Message: $"Breakpoint added at address 0x{address:X4}.");
                    }
                case "clear":
                    executionContext.Breakpoints.Clear();
                    return new GlobalCommandResult(Success: true, Message: $"All breakpoints have been removed.");
                case "list":
                    var breakpoints = executionContext.Breakpoints.GetAll();
                    var breakpointList = string.Join(" ", breakpoints.Select(bp => bp.Address));
                    executionContext.Output.Write($"[BP] ${breakpointList}");

                    if (breakpoints.Length == 0)
                    {
                        return new GlobalCommandResult(Success: true, Message: "No breakpoints set.");
                    }
                    else
                    {
                        breakpointList = string.Join(", ", breakpoints.Select(bp => $"0x{bp.Address:X4}"));
                        return new GlobalCommandResult(Success: true, Message: $"Current breakpoints at addresses: {breakpointList}");
                    }
                default:
                    return new GlobalCommandResult(Success: false, Message: $"The action '{action}' is not valid for the '{Name}' command. Use 'toggle' or 'remove'.");
            }
        }
    }
}
