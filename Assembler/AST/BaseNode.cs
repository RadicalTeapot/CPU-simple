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

        public static NodeSpan Exclude(NodeSpan from, NodeSpan other)
        {
            if (from.Line != other.Line)
            {
                throw new ArgumentException("Cannot exclude spans from different lines.");
            }
            if (other.StartColumn < from.StartColumn || other.StartColumn > from.EndColumn)
            {
                throw new ArgumentException("Span to exclude must be overlapping the source span.");
            }
            int newStart = from.StartColumn;
            int newEnd = other.StartColumn;
            return new NodeSpan(newStart, newEnd, from.Line);
        }
    }

    public abstract class BaseNode(NodeSpan span)
    {
        public NodeSpan Span { get; } = span;
    }
}
