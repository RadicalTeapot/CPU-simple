using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Analysis.Directives
{
    internal class StringNode : BaseAnalysisNode
    {
        public StringNode(DirectiveNode directive)
        {
            var operands = directive.GetOperands();
            if (operands is not DirectiveOperandSet.SingleStringOperand(var stringLiteral))
            {
                throw new AnalyserException("'string' directive requires a single string literal operand", directive.Span.Line, directive.Span.StartColumn);
            }
            var processedStr = OperandValueProcessor.ProcessString(stringLiteral.Value);
            var strBytes = System.Text.Encoding.ASCII.GetBytes(processedStr);
            EmitNodes = [new DataEmitNode([.. strBytes, NullTerminator])];
        }

        private const byte NullTerminator = 0x00;
    }
}
