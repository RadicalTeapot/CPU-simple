using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Analysis.Directives
{
    internal class ByteNode : BaseAnalysisNode
    {
        public ByteNode(DirectiveNode directive)
        {
            var operands = directive.GetOperands();
            if (operands is not DirectiveOperandSet.SingleHexNumberOperand(var byteOperand))
            {
                throw new AnalyserException("'byte' directive requires a single numeric operand", directive.Span.Line, directive.Span.StartColumn);
            }

            EmitNodes = [new DataEmitNode([OperandValueProcessor.ParseHexByteString(byteOperand.Value)], directive.Span)];
        }
    }
}
