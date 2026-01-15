using Assembler.Analysis.EmitNode;
using Assembler.AST;
using System.Diagnostics;

namespace Assembler.Analysis.Directives
{
    internal class ShortNode : BaseAnalysisNode
    {
        public ShortNode(DirectiveNode directive)
        {
            var operands = directive.GetOperands();
            if (operands is not DirectiveOperandSet.SingleHexNumberOperand(var shortOperand))
            {
                throw new AnalyserException("'short' directive requires a single numeric operand", directive.Span.Line, directive.Span.StartColumn);
            }

            Debug.Assert(BitConverter.IsLittleEndian, "This code assumes a little-endian architecture");
            EmitNodes = [
                new DataEmitNode(BitConverter.GetBytes(OperandValueProcessor.ParseHexUShortString(shortOperand.Value)), directive.Span)
            ];
        }
    }
}
