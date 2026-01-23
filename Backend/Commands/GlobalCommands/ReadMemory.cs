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
        protected override GlobalCommandResult ExecuteCore(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args)
        {
            if (args.Length > 2)
            {
                return new GlobalCommandResult(
                    Success: false,
                    Message: $"Error: '{Name}' command takes at most two arguments: start address (hex) and length (decimal).");
            }

            var memory = inspector.MemoryContents;
            var address = args.Length > 0 ? Convert.ToInt32(args[0], 16) : 0;
            var length = args.Length > 1 ? Convert.ToInt32(args[1]) : memory.Length;
                    
            length = Math.Min(length, memory.Length - address);

            var data = new byte[length];
            Array.Copy(memory, address, data, 0, length);
            var hexString = BitConverter.ToString(data).Replace("-", " ");
            output.Write($"[MEMORY] {hexString}");

            return new GlobalCommandResult(Success: true);
        }
    }
}
