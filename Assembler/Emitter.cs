using Assembler.Analysis.EmitNode;
using Assembler.AST;
using System.Diagnostics;

namespace Assembler
{
    public class EmitterException : Exception
    {
        public EmitterException(string message) : base(message) { }
    }

    public record SpanAddressInfo(NodeSpan Span, int StartAddress, int EndAddress);

    public class Emitter
    {
        public Emitter(int maxAddress = 0)
        {
            _maxAddress = (maxAddress == 0 || maxAddress > DefaultMaxAddress) ? DefaultMaxAddress : maxAddress;
            _memory = [];
            _spanAddresses = [];
        }

        public byte[] Emit(IList<IEmitNode> nodes)
        {
            Initialize();
            foreach (var node in nodes)
            {
                EmitNode(node);
            }
            return [.. _memory];
        }

        private void Initialize()
        {
            _memory.Clear();
            _programCounter = 0;
            _spanAddresses.Clear();
        }

        private void EmitNode(IEmitNode node)
        {
            var bytes = node.Emit();
            var count = node.Count;

            if (bytes.Length != count)
            {
                throw new EmitterException("EmitNode: byte array length does not match count.");
            }

            if (_programCounter + count > _maxAddress)
            {
                throw new EmitterException($"Memory overflow: attempting to write beyond max address {_maxAddress:X4}.");
            }

            _spanAddresses.Add(new SpanAddressInfo(node.Span, _programCounter, _programCounter + count - 1));
            _memory.AddRange(bytes);
            _programCounter += count;
        }

        public IList<SpanAddressInfo> GetSpanAddresses() => _spanAddresses.AsReadOnly();

        private readonly List<byte> _memory;
        private int _programCounter = 0;
        private readonly List<SpanAddressInfo> _spanAddresses;
        private readonly int _maxAddress = 0;

#if x16
        private const int DefaultMemorySize = 0xFF80; // Reserve some space for stack (127 bytes)
#else
        private const int DefaultMaxAddress = 0xF0; // Reserve some space for stack (15 bytes)
#endif
    }
}
