using Assembler.Analysis.EmitNode;
using System.Diagnostics;

namespace Assembler
{
    public class EmitterException : Exception
    {
        public EmitterException(string message) : base(message) { }
    }

    internal class Emitter
    {
        public Emitter(int maxAddressValue = 0)
        {
#if x16
            memory = new byte[65536]; // 64KB for 16-bit architecture
            written = new bool[65536];
            this.maxAddressValue = maxAddressValue == 0 ? 65535 : maxAddressValue;
#else
            memory = new byte[256]; // 256 bytes for 8-bit architecture
            written = new bool[256];
            this.maxAddressValue = maxAddressValue == 0 ? 255 : maxAddressValue;
#endif
            Array.Fill(memory, (byte)0x00);
            Array.Fill(written, false);
            programCounter = 0;
        }

        public void Emit(IList<IEmitNode> nodes)
        {
            foreach (var node in nodes)
            {
                EmitNode(node);
            }
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

            if (programCounter + count - 1 > maxAddressValue)
            {
                throw new EmitterException($"Memory overflow: attempting to write beyond address {maxAddressValue:X4}.");
            }

            Array.Copy(bytes, 0, memory, programCounter, count);
            programCounter += count;
        }

        private readonly byte[] memory;
        private readonly bool[] written;
        private int programCounter = 0;
        private readonly int maxAddressValue = 0;
    }
}
