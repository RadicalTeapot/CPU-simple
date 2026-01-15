using Assembler.AST;

namespace Assembler.Analysis.EmitNode
{
    public interface IEmitNode
    {
        public NodeSpan Span { get; }
        public int Count { get; }
        public byte[] Emit();
    }
}
