namespace Backend.Commands
{
    internal class ReadMemory
    {
        public const string Name = "readmem";
        private readonly int address;
        private readonly int length;
        public ReadMemory(string[] args)
        {
            if (args.Length > 2)
            {
                throw new ArgumentException("readmem command takes at most 2 arguments.");
            }
            address = args.Length > 0 ? Convert.ToInt32(args[0], 16) : 0;
            length = args.Length > 1 ? Convert.ToInt32(args[1]) : 0;
        }
        public void Execute(CPU.CpuInspector inspector)
        {
            var memory = inspector.MemoryContents;
            var length = this.length > 0 ? Math.Min(this.length, memory.Length - address) : memory.Length;
            var data = new byte[length];
            Array.Copy(memory, address, data, 0, length);
            var hexString = BitConverter.ToString(data).Replace("-", " ");
            Console.WriteLine($"Memory at 0x{address:X} ({length} bytes): {hexString}");
        }
    }
}
