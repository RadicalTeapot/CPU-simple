namespace Assembler.Analysis.EmitNode
{
    public interface IEmitNode
    {
        public int Count { get; }
        public byte[] Emit();
    }
}
