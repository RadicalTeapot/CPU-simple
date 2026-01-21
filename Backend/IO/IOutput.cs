using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.IO
{
    internal interface IOutput
    {
        void Write(string message);
        void WriteStatus(CPU.CpuInspector inspector);
    }

    // Output is done on STDOUT
    internal class Output : IOutput
    {
        public void Write(string message)
        {
            Console.Out.WriteLine(message);
        }

        public void WriteStatus(CPU.CpuInspector inspector)
        {
            var zeroFlag = inspector.ZeroFlag ? 1 : 0;
            var carryFlag = inspector.CarryFlag ? 1 : 0;

            var sb = new StringBuilder();
            sb.Append("[STATUS] ")
                .Append($"{inspector.Cycle} ")
                .Append($"{inspector.PC} ")
                .Append($"{inspector.SP} ")
                .Append($"{string.Join(" ", inspector.Registers)} ")
                .Append($"{zeroFlag} ")
                .Append($"{carryFlag}");
            Console.Out.WriteLine(sb.ToString());
        }
    }
}
