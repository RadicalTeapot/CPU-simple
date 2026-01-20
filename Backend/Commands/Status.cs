using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Backend.Commands
{
    internal class Status
    {
        public const string Name = "status";
        public static void Execute(CPU.CpuInspector inspector)
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
            Output.Write(sb.ToString());
        }
    }
}
