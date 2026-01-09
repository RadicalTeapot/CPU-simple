namespace Assembler.Analysis
{
    internal class Section
    {
        public int LocationCounter => Nodes.Sum(node => node.Count);
        public int StartAddress { get; set; } = 0;
        public IList<IAnalysisNode> Nodes { get; } = [];
    }
}
