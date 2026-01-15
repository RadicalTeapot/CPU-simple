using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Analysis.Directives
{
    internal class ZeroNode : BaseAnalysisNode
    {
        public ZeroNode(DirectiveNode directiveNode)
        {
            var operands = directiveNode.GetOperands();
            if (operands is not DirectiveOperandSet.SingleHexNumberOperand(var zeroCountOperand))
            {
                throw new AnalyserException("'zero' directive requires a single numeric operand", directiveNode.Span.Line, directiveNode.Span.StartColumn);
            }
            var zeroCount = OperandValueProcessor.ParseHexNumberString(zeroCountOperand.Value);
            EmitNodes = [new FillEmitNode(zeroCount, FillValue, directiveNode.Span)];
        }

        private const byte FillValue = 0x00;
    }
}
