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
        public int LocationCounter => Nodes.Sum(node => node.Count);
        public int StartAddress { get; set; } = 0;
        public IList<BaseAnalysisNode> Nodes { get; } = [];
    }
}
