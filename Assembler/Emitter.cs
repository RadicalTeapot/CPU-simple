using Assembler.Analysis.EmitNode;
using System.Diagnostics;

namespace Assembler
{
    public class EmitterException : Exception
    {
        public EmitterException(string message) : base(message) { }
    }

    public class Emitter
    {
        public Emitter(int memorySize = 0)
        {
#if x16
            _memorySize = memorySize == 0 || memorySize > 65536 ? 65536 : memorySize;
#else
            _memorySize = memorySize == 0 || memorySize > 256 ? 256 : memorySize;
#endif
            _memory = new byte[_memorySize];
            _written = new bool[_memorySize];
        }

        public byte[] Emit(IList<IEmitNode> nodes)
        {
            Initialize();
            foreach (var node in nodes)
            {
                EmitNode(node);
            }
            return _memory;
        }

        private void Initialize()
        {
            Array.Fill(_memory, (byte)0x00);
            Array.Fill(_written, false);
            _programCounter = 0;
        }

        private void EmitNode(IEmitNode node)
        {
            var bytes = node.Emit();
            var count = node.Count;
            Debug.Assert(bytes.Length == count, "EmitNode: byte array length does not match count.");

            if (_programCounter + count >= _memorySize)
            {
                throw new EmitterException($"Memory overflow: attempting to write beyond address {_memorySize:X4}.");
            }

            if (_written.AsSpan(_programCounter, count).Contains(true))
            {
                throw new EmitterException($"Memory overwrite detected between address {_programCounter:X4} and {_programCounter + count - 1:X4}.");
            }

            Array.Copy(bytes, 0, _memory, _programCounter, count);
            _programCounter += count;
        }

        private readonly byte[] _memory;
        private readonly bool[] _written;
        private int _programCounter = 0;
        private readonly int _memorySize = 0;
    }
}
