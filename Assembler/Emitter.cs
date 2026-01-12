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
            this.maxAddressValue = memorySize == 0 || memorySize > 65535 ? 65535 : (memorySize - 1);
#else
            this.memorySize = memorySize == 0 || memorySize > 256 ? 255 : (memorySize - 1);
#endif
            memory = new byte[this.memorySize];
            written = new bool[this.memorySize];
        }

        public byte[] Emit(IList<IEmitNode> nodes)
        {
            Initialize();
            foreach (var node in nodes)
            {
                EmitNode(node);
            }
            return memory;
        }

        private void Initialize()
        {
            Array.Fill(memory, (byte)0x00);
            Array.Fill(written, false);
            programCounter = 0;
        }

        private void EmitNode(IEmitNode node)
        {
            var bytes = node.Emit();
            var count = node.Count;
            Debug.Assert(bytes.Length == count, "EmitNode: byte array length does not match count.");

            if (written.AsSpan(programCounter, count).Contains(true))
            {
                throw new EmitterException($"Memory overwrite detected between address {programCounter:X4} and {programCounter + count - 1:X4}.");
            }

            if (programCounter + count > memorySize)
            {
                throw new EmitterException($"Memory overflow: attempting to write beyond address {memorySize:X4}.");
            }

            Array.Copy(bytes, 0, memory, programCounter, count);
            programCounter += count;
        }

        private readonly byte[] memory;
        private readonly bool[] written;
        private int programCounter = 0;
        private readonly int memorySize = 0;
    }
}
