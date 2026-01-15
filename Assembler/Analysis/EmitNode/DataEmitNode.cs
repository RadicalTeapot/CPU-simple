using Assembler.AST;

namespace Assembler.Analysis.EmitNode
{
    internal class DataEmitNode(byte[] data, NodeSpan span) : IEmitNode
    {
        public byte[] Data { get; } = data;
        public int Count { get; } = data.Length;
        public byte[] Emit() => Data;
        public NodeSpan Span { get; } = span;
        }
}
