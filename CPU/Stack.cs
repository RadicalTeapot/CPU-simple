namespace CPU
{
    public interface IStack
    {
        byte SP { get; }
        int Size { get; }
        //void Push(byte value);
        //byte Pop();
        void Reset();
    }

    internal class Stack: IStack
    {
        public byte SP { get; private set; }
        public int Size { get; }

        public Stack(byte[] memory) : this(memory, DEFAULT_STACK_SIZE, DEFAULT_START_ADDRESS) { }

        public Stack(byte[] memory, int stackSize, byte pointerStartAddress)
        {
            Size = stackSize;
            _memory = memory;
            _pointerStartAddress = pointerStartAddress;
            Reset();
        }

        public void Reset()
        {
            SP = _pointerStartAddress;
            Array.Clear(_memory, _pointerStartAddress - Size + 1, Size);
        }

        public void Push(byte value)
        {
            if (SP == Size - 1)
                throw new InvalidOperationException("Stack overflow");
            _memory[SP--] = value;
        }

        public byte Pop()
        {
            if (SP == _pointerStartAddress)
                throw new InvalidOperationException("Stack underflow");
            return _memory[++SP];
        }

        private readonly byte[] _memory;
        private readonly byte _pointerStartAddress;

        private const byte DEFAULT_START_ADDRESS = 0xFF;
        private const int DEFAULT_STACK_SIZE = 16;
    }

    internal static class StackDebugExtensions
    {
        public static void Dump(this Stack stack, byte[] memory)
        {
            for (int i = memory.Length - stack.Size; i < memory.Length; i++)
            {
                Console.Write($"{memory[i]:X2} ");
            }
            Console.WriteLine();
        }
    }
}
