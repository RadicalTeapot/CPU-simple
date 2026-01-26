using System.Text;

namespace Backend.IO
{
    public interface IOutput
    {
        void Write(string message);
        void WriteStatus(CPU.CpuInspector inspector);
        void WriteMemoryDump(byte[] memoryDump);
        void WriteStackDump(byte[] stackDump);
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
                .Append($"{carryFlag} ")
                .Append($"({string.Join(" ", inspector.MemoryChanges)}) ")
                .Append($"({string.Join(" ", inspector.StackChanges)})");
            Console.Out.WriteLine(sb.ToString());
        }

        public void WriteMemoryDump(byte[] memoryDump)
        {
            var hexString = BitConverter.ToString(memoryDump).Replace("-", " ");
            Console.Out.WriteLine($"[MEMORY] {hexString}");
        }

        public void WriteStackDump(byte[] stackDump)
        {
            var hexString = BitConverter.ToString(stackDump).Replace("-", " ");
            Console.Out.WriteLine($"[STACK] {hexString}");
        }
    }
}
