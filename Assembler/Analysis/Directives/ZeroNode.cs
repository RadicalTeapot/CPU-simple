using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Analysis.Directives
{
    internal class ZeroNode : IAnalysisNode
    {
        public ZeroNode(DirectiveNode directiveNode)
        {
            var operands = directiveNode.GetOperands();
            if (operands is not DirectiveOperandSet.SingleHexNumberOperand(var zeroCountOperand))
            {
                throw new AnalyserException("'zero' directive requires a single numeric operand", directiveNode.Span.Line, directiveNode.Span.StartColumn);
            }
            var zeroCount = OperandValueProcessor.ParseHexNumberString(zeroCountOperand.Value);
            emitNode = new FillEmitNode(zeroCount, FillValue);
        }

        public int Count => emitNode.Count;
        public byte[] EmitBytes() => emitNode.Emit();

        private readonly FillEmitNode emitNode;
        private const byte FillValue = 0x00;
    }
}
