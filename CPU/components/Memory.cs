namespace CPU.components
{
    public class Memory
    {
        public int Size { get; }
        
        public Memory(int size)
        {
            _memory = new byte[size];
            Size = size;
        }

        public byte ReadByte(int address)
        {
            if (address < 0 || address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            return _memory[address];
        }

        public void WriteByte(int address, byte value)
        {
            if (address < 0 || address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");
            _memory[address] = value;
        }

        public void LoadBytes(int address, byte[] data)
        {
            if (address < 0 || address + data.Length > Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory load address out of bounds: {address}.");
            Array.Copy(data, 0, _memory, address, data.Length);
        }

        public void Clear()
        {
            Array.Clear(_memory);
        }

        private readonly byte[] _memory;
    }

    internal static class MemoryDebugExtensions
    {
        public static void Dump(this Memory memory)
        {
            Console.WriteLine("Memory Dump:");
            const int columns = 16;
            var rows = memory.Size / columns;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    Console.Write($"{memory.ReadByte(i * columns + j):X2} ");
                }
                Console.WriteLine();
            }
        }
    }
}
