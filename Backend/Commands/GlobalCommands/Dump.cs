using Backend.CpuStates;
using Backend.IO;
using CPU;
using System.ComponentModel.DataAnnotations;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "dump",
        description: "Dump full cpu state",
        helpText: "Usage: 'dump [status] [memory] [stack]'")]
    internal class Dump(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(GlobalCommandExecutionContext executionContext, string[] args)
        {
            if (args.Length > 3)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command does takes at most 3 arguments.");
            }

            var dumpStatus = args.Length == 0;
            var dumpMemory = args.Length == 0;
            var dumpStack = args.Length == 0;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "status":
                        dumpStatus = true;
                        break;
                    case "memory":
                        dumpMemory = true;
                        break;
                    case "stack":
                        dumpStack = true;
                        break;
                    default:
                        return new GlobalCommandResult(Success: false, Message: $"The '{args[i]}' argument is not valid for the '{Name}' command.");
                }
            }

            if (dumpStatus)
                executionContext.Output.WriteStatus(executionContext.Inspector);
            if (dumpMemory)
                executionContext.Output.WriteMemoryDump(executionContext.Inspector.MemoryContents);
            if (dumpStack)
                executionContext.Output.WriteStackDump(executionContext.Inspector.StackContents);

            return new GlobalCommandResult(Success: true);
        }
    }
}
