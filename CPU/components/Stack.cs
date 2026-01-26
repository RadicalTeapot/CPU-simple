using System.Diagnostics;

namespace CPU.components
{
    public class Stack
    {
        public byte SP { get; private set; } // Note: Keep as byte for both 8-bit and 16-bit architectures, stack size is max 256 bytes
        public int Size => _memory.Size;

        public Stack(int stackSize): this(new Memory(stackSize), (byte)(stackSize - 1)) { }

        public Stack(Memory memory, byte pointerStartAddress)
        {
            Debug.Assert(memory.Size > 0 && memory.Size <= 256, "Stack size must be between 1 and 256 bytes.");
            Debug.Assert(pointerStartAddress < memory.Size, "Pointer start address must be within memory bounds.");
            _memory = memory;
            _pointerStartAddress = pointerStartAddress;
            Reset();
        }

        public void Reset()
        {
            SP = _pointerStartAddress;
            _memory.Clear();
        }

        public void PushByte(byte value, ExecutionContext executionContext)
        {
            if (SP == 0)
                throw new InvalidOperationException("Stack overflow");
            _memory.WriteByte(SP--, value, executionContext.RecordStackChange);
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
        private void PushWord(ushort value, ExecutionContext executionContext)
        {
            if (SP < 2)
                throw new InvalidOperationException("Stack overflow");
            _memory.WriteByte(SP--, (byte)((value >> 8) & 0xFF), executionContext.RecordStackChange);
            _memory.WriteByte(SP--, (byte)(value & 0xFF), executionContext.RecordStackChange);            
        }

        private ushort PopWord()
        {
            if (SP > _pointerStartAddress - 2)
                throw new InvalidOperationException("Stack underflow");
            ushort low = _memory.ReadByte(++SP);
            ushort high = _memory.ReadByte(++SP);            
            return (ushort)(low | (high << 8));
        }

        private ushort PeekWord()
        {
            if (SP > _pointerStartAddress - 2)
                throw new InvalidOperationException("Stack underflow");
            ushort low = _memory.ReadByte(SP + 1);
            ushort high = _memory.ReadByte(SP + 2);
            return (ushort)(low | (high << 8));
        }

        public void PushAddress(ushort value, ExecutionContext executionContext) => PushWord(value, executionContext);
        public ushort PopAddress(ExecutionContext executionContext) => PopWord(executionContext);
        public ushort PeekAddress() => PeekWord();
#else
        public void PushAddress(byte value, ExecutionContext executionContext) => PushByte(value, executionContext);
        public byte PopAddress() => PopByte();
        public byte PeekAddress() => PeekByte();
#endif

        // Used for debugging (see StackDebugExtensions)
        public byte PeekByte(int offset)
        {
            if (offset < 0 || offset >= Size)
                throw new ArgumentOutOfRangeException(nameof(offset), $"Stack read offset out of bounds: {offset}.");
            return _memory.ReadByte(offset);
        }

        internal void UpdateCpuInspectorBuilder(CpuInspector.Builder inspectorBuilder)
        {
            inspectorBuilder.SetSP(SP);
            var stackBytes = new byte[Size];
            for (int i = 0; i < Size; i++)
            {
                stackBytes[i] = _memory.ReadByte(i);
            }
            inspectorBuilder.SetStack(stackBytes);
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
                Console.Write($"{stack.PeekByte(i):X2} ");
            }
            Console.WriteLine();
        }
    }
}
