namespace CPU.components
{
    public class Stack
    {
        public byte SP { get; private set; }
        public int Size => _memory.Size;

        public Stack(int stackSize, byte pointerStartAddress)
        {
            _memory = new Memory(stackSize);
            _pointerStartAddress = pointerStartAddress;
            Reset();
        }

        public void Reset()
        {
            SP = _pointerStartAddress;
            _memory.Clear();
        }

        public void Push(byte value)
        {
            if (SP == Size - 1)
                throw new InvalidOperationException("Stack overflow");
            _memory.WriteByte(SP--, value);
        }

        public byte Pop()
        {
            if (SP == _pointerStartAddress)
                throw new InvalidOperationException("Stack underflow");
            return _memory.ReadByte(++SP);
        }

        public byte ReadByte(int offset)
        {
            if (offset < 0 || offset >= Size)
                throw new ArgumentOutOfRangeException(nameof(offset), $"Stack read offset out of bounds: {offset}.");
            return _memory.ReadByte(offset);
        }

        private readonly Memory _memory;
        private readonly byte _pointerStartAddress;
    }

    internal static class StackDebugExtensions
    {
        public static void Dump(this Stack stack)
        {
            Console.WriteLine("Stack Dump:");
            for (int i = 0; i < stack.Size; i++)
            {
                Console.Write($"{stack.ReadByte(i):X2} ");
            }
            Console.WriteLine();
        }
    }
}
