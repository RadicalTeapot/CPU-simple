using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Analysis.Directives
{
    internal class OrgNode : BaseAnalysisNode
    {
        public OrgNode(DirectiveNode directive, int currentLocationCounter, MemoryAddressValueProcessor memoryAddressValueProcessor)
        {
            byte fillValue;
            int address;
            var operands = directive.GetOperands();
            switch (operands)
            {
                case DirectiveOperandSet.SingleHexNumberOperand(var addressOperand):
                    address = memoryAddressValueProcessor.ParseAddressValueString(addressOperand);
                    fillValue = DefaultFillValue;
                    break;
                case DirectiveOperandSet.PairOfImmediateValueOperands(var addressOperand, var fillValueOperand):
                    address = memoryAddressValueProcessor.ParseAddressValueString(addressOperand);
                    fillValue = OperandValueProcessor.ParseHexByteString(fillValueOperand.Value);
                    break;
                default:
                    throw new AnalyserException("'org' directive requires an address operand", directive.Span.Line, directive.Span.StartColumn);
            }

            var bytesToFill = address - currentLocationCounter;
            EmitNodes = [new FillEmitNode(bytesToFill, fillValue, directive.Span)];
        }

        private const byte DefaultFillValue = 0x00;
    }
}
