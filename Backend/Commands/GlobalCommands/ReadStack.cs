namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "readstack", ["rds"],
        description: "Reads parts or whole stack",
        helpText: "Usage: 'readstack [startaddress [length]]'")]
    internal class ReadStack(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length > 2)
            {
                return new GlobalCommandResult(
                    Success: false,
                    Message: $"Error: '{Name}' command takes at most two arguments: start address (hex) and length (decimal).");
            }

            var sp = executionContext.Inspector.SP;
            var stack = executionContext.Inspector.StackContents;

            int address;
            int length;
            try
            {
                address = args.Length > 0 ? Convert.ToInt32(args[0], 16) : sp;
                length = args.Length > 1 ? Convert.ToInt32(args[1]) : stack.Length;
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                return new GlobalCommandResult(Success: false, Message: $"Error: Invalid argument format. {ex.Message}");
            }

            length = Math.Min(length, address+1); // Stack grows downwards, so we can't read more than address+1 bytes

            if (length <= 0 || address < 0 || address >= stack.Length)
            {
                return new GlobalCommandResult(
                    Success: false,
                    Message: $"Error: Invalid start address or length.");
            }

            var data = new byte[length];
            Array.Copy(stack, address - length + 1, data, 0, length); // Stack grows downwards so we read backwards
            var hexString = BitConverter.ToString(data).Replace("-", " ");

            return new GlobalCommandResult(Success: true, Message: $"[STACK] SP: {executionContext.Inspector.SP:X2} Address: {address:X2} Content: {hexString}");
        }
    }
}
