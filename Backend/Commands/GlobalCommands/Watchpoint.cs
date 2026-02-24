using CPU.microcode;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "watchpoint", ["wp"],
        description: "Add, remove, or list watchpoints",
        helpText: "Usage: 'watchpoint [on-write/on-read <address> | on-phase <phase> | remove <id> | clear | list]'")]
    internal class Watchpoint(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command takes one or two arguments.");

            var action = args[0].ToLower();
            string resultMessage;
            switch (action)
            {
                case "on-write":
                    if (args.Length < 2)
                        return new GlobalCommandResult(Success: false, Message: "The 'on-write' action requires an address argument.");
                    if (!int.TryParse(args[1], out int writeAddress) || writeAddress < 0 || writeAddress > 0xFFFF)
                        return new GlobalCommandResult(Success: false, Message: $"The address '{args[1]}' is not a valid memory address.");
                    var writeWp = new AddressWatchpoint(executionContext.Watchpoints.NextId(), BusDirection.Write, writeAddress);
                    executionContext.Watchpoints.Add(writeWp);
                    resultMessage = $"Watchpoint {writeWp.Id} added: {writeWp.Description}";
                    break;

                case "on-read":
                    if (args.Length < 2)
                        return new GlobalCommandResult(Success: false, Message: "The 'on-read' action requires an address argument.");
                    if (!int.TryParse(args[1], out int readAddress) || readAddress < 0 || readAddress > 0xFFFF)
                        return new GlobalCommandResult(Success: false, Message: $"The address '{args[1]}' is not a valid memory address.");
                    var readWp = new AddressWatchpoint(executionContext.Watchpoints.NextId(), BusDirection.Read, readAddress);
                    executionContext.Watchpoints.Add(readWp);
                    resultMessage = $"Watchpoint {readWp.Id} added: {readWp.Description}";
                    break;

                case "on-phase":
                    if (args.Length < 2)
                        return new GlobalCommandResult(Success: false, Message: "The 'on-phase' action requires a phase argument.");
                    if (!Enum.TryParse<MicroPhase>(args[1], ignoreCase: true, out var phase))
                        return new GlobalCommandResult(Success: false, Message: $"'{args[1]}' is not a valid MicroPhase. Valid values: {string.Join(", ", Enum.GetNames<MicroPhase>())}");
                    var phaseWp = new PhaseWatchpoint(executionContext.Watchpoints.NextId(), phase);
                    executionContext.Watchpoints.Add(phaseWp);
                    resultMessage = $"Watchpoint {phaseWp.Id} added: {phaseWp.Description}";
                    break;

                case "remove":
                    if (args.Length < 2)
                        return new GlobalCommandResult(Success: false, Message: "The 'remove' action requires a watchpoint id argument.");
                    if (!int.TryParse(args[1], out int removeId) || removeId < 0)
                        return new GlobalCommandResult(Success: false, Message: $"'{args[1]}' is not a valid watchpoint id.");
                    executionContext.Watchpoints.Remove(removeId);
                    resultMessage = $"Watchpoint {removeId} removed.";
                    break;

                case "clear":
                    executionContext.Watchpoints.Clear();
                    resultMessage = "All watchpoints have been removed.";
                    break;

                case "list":
                    if (executionContext.Watchpoints.Count == 0)
                        resultMessage = "No watchpoints set.";
                    else
                    {
                        var descriptions = executionContext.Watchpoints.GetAll().Select(wp => $"[{wp.Id}] {wp.Description}");
                        resultMessage = $"Current watchpoints: {string.Join(", ", descriptions)}";
                    }
                    break;

                default:
                    return new GlobalCommandResult(Success: false, Message: $"The action '{action}' is not valid for the '{Name}' command. Use 'on-write', 'on-read', 'on-phase', 'remove', 'clear', or 'list'.");
            }

            executionContext.Output.WriteWatchpointList(executionContext.Watchpoints.GetAll());
            return new GlobalCommandResult(Success: true, Message: resultMessage);
        }
    }
}
