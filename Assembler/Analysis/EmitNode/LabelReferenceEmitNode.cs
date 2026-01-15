using Assembler.AST;

namespace Assembler.Analysis.EmitNode
{
    internal class LabelReferenceEmitNode(LabelReferenceNode labelRefNode, int offset, NodeSpan span) : IEmitNode
    {
        public LabelReferenceNode LabelRefNode { get; } = labelRefNode;
        public int Offset { get; } = offset;
        public NodeSpan Span { get; } = span;
#if x16
        public int Count { get; } = 2;
#else
        public int Count { get; } = 1;
#endif

        public void Resolve(int value)
        {
            var valueWithOffset = value + Offset;
#if x16
            if (valueWithOffset < 0 || valueWithOffset > 65535)
            {
                throw new ParserException($"Label '{LabelRefNode.Label}' resolved address {valueWithOffset:X4} with offset {Offset} is out of range for 16-bit addressing.", LabelRefNode.Span.Line, LabelRefNode.Span.StartColumn);
            }
            resolvedValue = (ushort)valueWithOffset;
#else
            if (valueWithOffset < 0 || valueWithOffset > 255)
            {
                throw new ParserException($"Label '{LabelRefNode.Label}' resolved address {valueWithOffset:X2} with offset {Offset} is out of range for 8-bit addressing.", LabelRefNode.Span.Line, LabelRefNode.Span.StartColumn);
            }
            resolvedValue = (byte)valueWithOffset;
#endif
            isResolved = true;
        }

        public byte[] Emit()
        {
            if (!isResolved)
            {
                throw new ParserException($"Label '{LabelRefNode.Label}' has not been resolved yet.", LabelRefNode.Span.Line, LabelRefNode.Span.StartColumn);
            }
#if x16
            Debug.Assert(BitConverter.IsLittleEndian, "This code assumes a little-endian architecture");
            return BitConverter.GetBytes(resolvedValue);
#else
            return [resolvedValue];
#endif
        }

        private bool isResolved = false;
#if x16
        private ushort resolvedValue;
#else
        private byte resolvedValue;
#endif
    }
}
