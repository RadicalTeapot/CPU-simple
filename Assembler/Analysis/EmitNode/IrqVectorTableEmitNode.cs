using Assembler.AST;

namespace Assembler.Analysis.EmitNode
{
    /// <summary>
    /// Emits the IRQ vector table: a 1-byte (8-bit) or 2-byte (16-bit) entry containing
    /// the address of the IRQ handler, placed at the fixed vector table address in memory.
    /// The CPU reads this table at interrupt time to discover where the handler is located.
    /// </summary>
    internal class IrqVectorTableEmitNode(int handlerAddress, NodeSpan span) : IEmitNode
    {
        public NodeSpan Span { get; } = span;
#if x16
        public int Count { get; } = 2;
        public byte[] Emit() => [(byte)(handlerAddress & 0xFF), (byte)(handlerAddress >> 8)];
#else
        public int Count { get; } = 1;
        public byte[] Emit() => [(byte)handlerAddress];
#endif
    }
}
