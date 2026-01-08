namespace Assembler.AST
{
    public readonly struct NodeSpan(int start, int end, int line)
    {
        /// <summary>
        /// 0-based node start column index
        /// </summary>
        public int StartColumn { get; } = start;
        /// <summary>
        /// 0-based node end column index
        /// </summary>
        public int EndColumn { get; } = end;
        /// <summary>
        /// 0-based line index
        /// </summary>
        /// <remarks>Multi-line nodes are not supported by the language</remarks>
        public int Line { get; } = line;
    }

    public abstract class BaseNode(NodeSpan span)
    {
        public NodeSpan Span { get; } = span;
    }
}
