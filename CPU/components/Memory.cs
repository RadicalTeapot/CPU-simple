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

#if x16
        public byte ReadByte(ushort address)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");

            return _memory[address];
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");

            _memory[address] = value;
        }

        public ushort ReadAddress(ushort address, out byte size)
        {
            if (address + 1 >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");

            size = 2;
            return (ushort)(_memory[address] | (_memory[address + 1] << 8));
        }

        public void WriteAddress(ushort address, ushort value)
        {
            if (address + 1 >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");

            _memory[address] = (byte)(value & 0xFF);
            _memory[address + 1] = (byte)((value >> 8) & 0xFF);
        }
#else
        public byte ReadByte(byte address)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            return _memory[address];
        }

        public void WriteByte(byte address, byte value)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");
            _memory[address] = value;
        }

        public byte ReadAddress(byte address, out byte size)
        {
            size = 1;
            return ReadByte(address);
        }

        public void WriteAddress(byte address, byte value) => WriteByte(address, value);
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
