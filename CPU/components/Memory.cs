namespace CPU.components
{
    public class Memory(int size)
    {
        public int Size { get; } = size;

#if x16
        public byte ReadByte(ushort address)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");

            return _memory[address];
        }

        public byte[] ReadBytes(ushort address, int length)
        {
            if (address + length > Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            byte[] result = new byte[length];
            Array.Copy(_memory, address, result, 0, length);
            return result;
        }

        public void WriteByte(ushort address, byte value) => WriteByte(address, value, null);
        public void WriteByte(ushort address, byte value, ExecutionContext executionContext)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");

            _memory[address] = value;
            executionContext.RecordMemoryChange(address, value);
        }
#else
        public byte ReadByte(byte address)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            return _memory[address];
        }

        public byte[] ReadBytes(byte address, int length)
        {
            if (address + length > Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            byte[] result = new byte[length];
            Array.Copy(_memory, address, result, 0, length);
            return result;
        }

        public void WriteByte(byte address, byte value) => WriteByte(address, value, null);
        public void WriteByte(byte address, byte value, Action<int, byte>? reporter)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");
            _memory[address] = value;
            reporter?.Invoke(address, value);
        }
#endif

        // Used for debugging (see MemoryDebugExtensions)
        public byte ReadByte(int address)
        {
            if (address < 0 || address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            return _memory[address];
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

        internal void UpdateCpuInspectorBuilder(CpuInspector.Builder inspectorBuilder)
        {
            var memory = new byte[_memory.Length];
            Array.Copy(_memory, memory, _memory.Length); // Create a copy to avoid external modifications
            inspectorBuilder.SetMemory(memory);
        }

        private readonly byte[] _memory = new byte[size];
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
