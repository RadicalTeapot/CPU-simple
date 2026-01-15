using Assembler.AST;

namespace Assembler.Analysis.EmitNode
{
    internal class FillEmitNode(int count, byte fillValue, NodeSpan span) : IEmitNode
    {
        public int Count { get; } = count;
        public byte FillValue { get; } = fillValue;
        public NodeSpan Span { get; } = span;
        public byte[] Emit() => [.. Enumerable.Repeat(FillValue, Count)];
    }
}
