using Assembler.AST;

namespace Assembler.Analysis.EmitNode
{
    internal class LabelReferenceEmitNode(LabelReferenceNode labelRefNode, int offset) : IEmitNode
    {
        public LabelReferenceNode LabelRefNode { get; } = labelRefNode;
        public int Offset { get; } = offset;
        public int Count { get; } = 1;

        public void Resolve(byte value)
        {
            resolvedValue = (byte)(value + Offset); // TODO Handle overflow? And handle 16-bit labels?
            isResolved = true;
        }

        public byte[] Emit()
        {
            if (!isResolved)
            {
                throw new ParserException($"Label '{LabelRefNode.Label}' has not been resolved yet.", LabelRefNode.Span.Line, LabelRefNode.Span.StartColumn);
            }
            return [resolvedValue];
        }

        private bool isResolved = false;
        private byte resolvedValue;
    }
}
