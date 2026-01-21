using Backend.CpuStates;
using Backend.IO;
using CPU;
using System.Text;

namespace Backend.Commands.GlobalCommands
{
    [Command(CommandType.Global, "status", ["state"],
        description: "Display the current CPU state",
        helpText: "Usage: 'status'")]
    internal class Status(CommandContext context) : IGlobalCommand
    {
        public string Name { get => context.Name;  }

        public string Description { get => context.Description; }

        public string HelpText { get => context.HelpText; }

        public GlobalCommandResult Execute(CpuInspector inspector, ICpuState currentState, string[] args)
        {
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
            new Output().Write(sb.ToString());
            return new GlobalCommandResult(Success: true);
        }
    }
}
