namespace Assembler.Analysis.EmitNode
{
    internal class DataEmitNode(byte[] data) : IEmitNode
    {
        public byte[] Data { get; } = data;
        public int Count { get; } = data.Length;
        public byte[] Emit() => Data;
    }
}
