namespace Assembler.Analysis
{
    public interface IAnalysisNode
    {
        int Count { get; }
        byte[] EmitBytes();
    }
}
