using System.Text;

namespace Backend.IO
{
    public interface IOutput
    {
        void Write(string message);
        void WriteStatus(CPU.CpuInspector inspector);
    }

    // Output is done on STDOUT
    internal class ConsoleOutput : IOutput
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
