using System.Diagnostics;

namespace CPU.components
{
    public class Stack
    {
        public byte SP { get; private set; } // Note: Keep as byte for both 8-bit and 16-bit architectures, stack size is max 256 bytes
        public int Size => _memory.Size;

        public Stack(int stackSize)
        {
            Debug.Assert(stackSize > 0 && stackSize <= 256, "Stack size must be between 1 and 256 bytes.");
            _memory = new Memory(stackSize);
            _pointerStartAddress = (byte)(stackSize - 1);
            Reset();
        }

        public void Reset()
        {
            SP = _pointerStartAddress;
            _memory.Clear();
        }

        public void PushByte(byte value)
        {
            if (SP == 0)
                throw new InvalidOperationException("Stack overflow");
            _memory.WriteByte(SP--, value);
        }

        public byte PopByte()
        {
            if (SP == _pointerStartAddress)
                throw new InvalidOperationException("Stack underflow");
            return _memory.ReadByte(++SP);
        }
        public byte PeekByte()
        {
            if (SP == _pointerStartAddress)
                throw new InvalidOperationException("Stack underflow");
            return _memory.ReadByte(SP + 1);
        }

#if x16
        public void PushAddress(ushort value)
        {
            if (SP < 2)
                throw new InvalidOperationException("Stack overflow");
            _memory.WriteByte((ushort)(SP - 1), (byte)(value & 0xFF));
            _memory.WriteByte(SP--, (byte)((value >> 8) & 0xFF));
        }
        public ushort PopAddress()
        {
            if (SP > _pointerStartAddress - 2)
                throw new InvalidOperationException("Stack underflow");
            ushort high = _memory.ReadByte(++SP);
            ushort low = _memory.ReadByte(++SP);
            return (ushort)(low | (high << 8));
        }
        public ushort PeekAddress()
        {
            if (SP > _pointerStartAddress - 2)
                throw new InvalidOperationException("Stack underflow");
            ushort high = _memory.ReadByte(SP + 1);
            ushort low = _memory.ReadByte(SP + 2);
            return (ushort)(low | (high << 8));
        }
#else
        public void PushAddress(byte value) => PushByte(value);
        public byte PopAddress() => PopByte();
        public byte PeekAddress() => PeekByte();
#endif

        // Used for debugging (see StackDebugExtensions)
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
