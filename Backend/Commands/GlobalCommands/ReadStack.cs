using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "readstack", ["rds"],
        description: "Reads parts or whole stack",
        helpText: "Usage: 'readstack [startaddress [length]]'")]
    internal class ReadStack(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args)
        {
            if (args.Length > 2)
            {
                return new GlobalCommandResult(
                    Success: false,
                    Message: $"Error: '{Name}' command takes at most two arguments: start address (hex) and length (decimal).");
            }

            var sp = inspector.SP;
            var stack = inspector.StackContents;

            var address = args.Length > 0 ? Convert.ToInt32(args[0], 16) : sp;
            var length = args.Length > 1 ? Convert.ToInt32(args[1]) : stack.Length;
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
            output.Write($"[STACK] {hexString}");

            return new GlobalCommandResult(Success: true);
        }
    }
}
