using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Analysis.Directives
{
    internal class ByteNode : IAnalysisNode
    {
        public ByteNode(DirectiveNode directive)
        {
            var operands = directive.GetOperands();
            if (operands is not DirectiveOperandSet.SingleHexNumberOperand(var byteOperand))
            {
                throw new AnalyserException("'byte' directive requires a single numeric operand", directive.Span.Line, directive.Span.StartColumn);
            }

            emitNode = new DataEmitNode([OperandValueProcessor.ParseHexByteString(byteOperand.Value)]);
        }

        public int Count => emitNode.Count;
        public byte[] EmitBytes() => emitNode.Emit();

        private readonly DataEmitNode emitNode;
    }
}
