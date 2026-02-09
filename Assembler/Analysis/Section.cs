namespace Assembler.Analysis
{
    internal class Section(Section.Type sectionType)
    {
        public enum Type
        {
            Text,
            Data
        }
        public Type SectionType { get; } = sectionType;
        // Note: O(n) recalculated on every access. Could be optimized to a cached
        // counter incremented in an AddNode() method if performance becomes a concern.
        public int LocationCounter => Nodes.Sum(node => node.Count);
        public int StartAddress { get; set; } = 0;
        public IList<BaseAnalysisNode> Nodes { get; } = [];
    }
}
