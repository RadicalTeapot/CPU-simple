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
        protected override GlobalCommandResult ExecuteCore(CpuInspector inspector, ICpuState currentState, IOutput output, string[] args)
        {
            if (args.Length != 0)
            {
                return new GlobalCommandResult(Success: false, Message: $"The '{Name}' command does not take any arguments.");
            }

            var sb = new StringBuilder();
            sb.Append($"Cycle: {inspector.Cycle} ");
            sb.Append($"PC: 0x{inspector.PC:X2} ");
            sb.Append($"SP: 0x{inspector.SP:X2} ");
            for (int i = 0; i < inspector.Registers.Length; i++)
            {
                sb.Append($"R{i}: 0x{inspector.Registers[i]:X2} ");
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
            output.Write(sb.ToString());
            return new GlobalCommandResult(Success: true);
        }
    }
}
