using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Analysis.Directives
{
    internal class StringNode : IAnalysisNode
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
            emitNode = new DataEmitNode([.. strBytes, 0x00]);
        }

        public int Count => emitNode.Count;
        public byte[] EmitBytes() => emitNode.Emit();

        private readonly DataEmitNode emitNode;
    }
}
