namespace Assembler.Analysis.EmitNode
{
    internal class DataEmitNode(byte[] data)
    {
        public byte[] Data { get; } = data;
        public int Count { get; } = data.Length;
        public byte[] Emit() => Data;
    }
}
