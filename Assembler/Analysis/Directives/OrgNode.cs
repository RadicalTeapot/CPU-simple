using Assembler.Analysis.EmitNode;
using Assembler.AST;


namespace Assembler.Analysis.Directives
{
    internal class OrgNode : IAnalysisNode
    {
        public OrgNode(DirectiveNode directive, int currentLocationCounter)
        {
            byte fillValue;
            int address;
            var operands = directive.GetOperands();
            switch (operands)
            {
                case DirectiveOperandSet.SingleHexNumberOperand(var addressOperand):
                    address = BitConverter.ToInt32(OperandValueProcessor.ParseAddressValueString(addressOperand));
                    fillValue = DefaultFillValue;
                    break;
                case DirectiveOperandSet.TwoHexNumberOperands(var addressOperand, var fillValueOperand):
                    address = BitConverter.ToInt32(OperandValueProcessor.ParseAddressValueString(addressOperand));
                    fillValue = OperandValueProcessor.ParseHexByteString(fillValueOperand.Value);
                    break;
                default:
                    throw new AnalyserException("'org' directive requires an address operand", directive.Span.Line, directive.Span.StartColumn);
            }

            var bytesToFill = address - currentLocationCounter;
            emitNode = new FillEmitNode(bytesToFill, fillValue);
        }

        public int Count => emitNode.Count;
        public byte[] EmitBytes() => emitNode.Emit();

        private readonly FillEmitNode emitNode;
        private const byte DefaultFillValue = 0x00;
    }
}
