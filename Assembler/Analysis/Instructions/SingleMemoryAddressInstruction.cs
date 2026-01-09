using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class SingleMemoryAddressInstruction : IAnalysisNode
    {
        public SingleMemoryAddressInstruction(InstructionNode instruction, OpcodeBaseCode opcode, LabelReferenceManager labelRefManager)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.SingleMemoryAddressOperand(var addressOperand))
            {
                throw new AnalyserException($"'{mnemonic}' instruction requires a single memory address operand", instruction.Span.Line, instruction.Span.StartColumn);
            }

            var memoryAddress = addressOperand.GetAddress();
            switch (memoryAddress)
            {
                case MemoryAddress.Immediate(var hexAddress):
                    var addressValue = OperandValueProcessor.ParseAddressValueString(hexAddress);
                    emitNode = new DataEmitNode([ (byte)opcode, ..addressValue ]);
                    labelRefNode = null;
                    break;
                case MemoryAddress.Label(var labelReference):
                    emitNode = new DataEmitNode([ (byte)opcode ]);
                    labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReference);
                    break;
                case MemoryAddress.LabelWithPositiveOffset(var labelReference, var offset):
                    emitNode = new DataEmitNode([ (byte)opcode ]);
                    var positiveOffset = OperandValueProcessor.ParseHexNumberString(offset.Value);
                    labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReference, positiveOffset);
                    break;
                case MemoryAddress.LabelWithNegativeOffset(var labelReference, var offset):
                    emitNode = new DataEmitNode([ (byte)opcode ]);
                    var negativeOffset = OperandValueProcessor.ParseHexNumberString(offset.Value) * -1;
                    labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReference, negativeOffset);
                    break;
                default:
                    throw new AnalyserException($"Invalid memory address operand for '{mnemonic}' instruction", instruction.Span.Line, instruction.Span.StartColumn);
            }
        }

        public int Count => emitNode.Count + (labelRefNode?.Count ?? 0);
        public byte[] EmitBytes() => [..emitNode.Emit(), ..(labelRefNode?.Emit() ?? [])];

        private readonly DataEmitNode emitNode;
        private readonly LabelReferenceEmitNode? labelRefNode;
    }
}
