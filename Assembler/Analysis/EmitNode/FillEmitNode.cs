namespace Assembler.Analysis.EmitNode
{
    internal class FillEmitNode(int count, byte fillValue) : IEmitNode
    {
        public int Count { get; } = count;
        public byte FillValue { get; } = fillValue;
        public byte[] Emit() => [.. Enumerable.Repeat(FillValue, Count)];
    }
}
