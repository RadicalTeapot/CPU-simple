using CPU.microcode;

namespace CPU.components
{
    public class Memory(int size)
    {
        public int Size { get; } = size;

        internal BusRecorder? Recorder { get; set; }

#if x16
        public byte ReadByte(ushort address)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");

            var value = _memory[address];
            Recorder?.RecordRead(address, value, BusType.Memory);
            return value;
        }

        public byte[] ReadBytes(ushort address, int length)
        {
            if (address + length > Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            byte[] result = new byte[length];
            Array.Copy(_memory, address, result, 0, length);
            return result;
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");

            _memory[address] = value;
            Recorder?.RecordWrite(address, value, BusType.Memory);
            reporter?.Invoke(address, value);
        }
#else
        public byte ReadByte(byte address)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            var value = _memory[address];
            Recorder?.RecordRead(address, value, BusType.Memory);
            return value;
        }

        public byte[] ReadBytes(byte address, int length)
        {
            if (address + length > Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory read address out of bounds: {address}.");
            byte[] result = new byte[length];
            Array.Copy(_memory, address, result, 0, length);
            return result;
        }

        public void WriteByte(byte address, byte value)
        {
            if (address >= Size)
                throw new ArgumentOutOfRangeException(nameof(address), $"Memory write address out of bounds: {address}.");
            _memory[address] = value;
            Recorder?.RecordWrite(address, value, BusType.Memory);
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
