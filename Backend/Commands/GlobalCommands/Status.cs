using Backend.CpuStates;
using Backend.IO;
using CPU;
using System.Text;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "status", ["state"],
        description: "Display the current CPU state")]
    internal class Status(CommandContext context) : BaseGlobalCommand(context)
    {
        protected override GlobalCommandResult ExecuteCore(ExecutionContext executionContext, string[] args)
        {
            if (args.Length != 0)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command does not take any arguments.");
            }

            var inspector = executionContext.Inspector;
            var sb = new StringBuilder("[STATUS] ");
            sb.Append($"Cycle: {inspector.Cycle} ");
            sb.Append($"PC: {inspector.PC:X2} ");
            sb.Append($"SP: {inspector.SP:X2} ");
            for (int i = 0; i < inspector.Registers.Length; i++)
            {
                sb.Append($"R{i}: {inspector.Registers[i]:X2} ");
            }
            sb.Append($"Zero: {inspector.ZeroFlag} ");
            sb.Append($"Carry: {inspector.CarryFlag} ");
            if (inspector.LastInstruction.Length > 0)
            {
                sb.Append("Last Instruction: ");
                for (int i = 0; i < inspector.LastInstruction.Length; i++)
                {
                    sb.Append($"{inspector.LastInstruction[i]} ");
                }
            }
            else
            {
                sb.Append("Last Instruction: N/A ");
            }
            if (inspector.MemoryChanges.Length > 0)
            {
                sb.Append("Memory Changes: ");
                foreach (var (address, value) in inspector.MemoryChanges)
                {
                    sb.Append($"[{address:X2}]: {value} ");
                }
            }
            else
            {
                sb.Append("Memory Changes: N/A ");
            }
            if (inspector.StackChanges.Length > 0)
            {
                sb.Append("Stack Changes: ");
                foreach (var (address, value) in inspector.StackChanges)
                {
                    sb.Append($"[{address:X2}]: {value} ");
                }
            }
            else
            {
                sb.Append("Stack Changes: N/A ");
            }
            
            return new GlobalCommandResult(Success: true, Message: sb.ToString());
        }
    }
}
