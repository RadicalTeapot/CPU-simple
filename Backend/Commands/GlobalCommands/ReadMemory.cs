using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "readmem", ["rdm"],
        description: "Reads parts or whole memory",
        helpText: "Usage: 'readmem [startaddress [length]]'")]
    internal class ReadMemory(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length > 2)
            {
                return new GlobalCommandResult(
                    Success: false,
                    Message: $"Error: '{Name}' command takes at most two arguments: start address (hex) and length (decimal).");
            }

            var memory = executionContext.Inspector.MemoryContents;

            int address;
            int length;
            try
            {
                address = args.Length > 0 ? Convert.ToInt32(args[0], 16) : 0;
                length = args.Length > 1 ? Convert.ToInt32(args[1]) : memory.Length;
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                return new GlobalCommandResult(Success: false, Message: $"Error: Invalid argument format. {ex.Message}");
            }

            if (address < 0 || address >= memory.Length)
            {
                return new GlobalCommandResult(Success: false, Message: $"Error: Address 0x{address:X2} is out of bounds (memory size: {memory.Length}).");
            }

            length = Math.Max(0, Math.Min(length, memory.Length - address));

            var data = new byte[length];
            Array.Copy(memory, address, data, 0, length);
            var hexString = BitConverter.ToString(data).Replace("-", " ");

            return new GlobalCommandResult(Success: true, Message: $"[MEMORY] Address: {address:X2} Content: {hexString}");
        }
    }
}
