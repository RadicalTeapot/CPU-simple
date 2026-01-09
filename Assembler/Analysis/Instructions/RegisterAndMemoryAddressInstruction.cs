using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;
using System.Windows.Markup;

namespace Assembler.Analysis.Instructions
{
    internal class RegisterAndMemoryAddressInstruction : IAnalysisNode
    {
        public RegisterAndMemoryAddressInstruction(InstructionNode instruction, OpcodeBaseCode opcode, LabelReferenceManager labelRefManager)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.RegisterAndMemoryAddressOperand(var registerOperand, var addressOperand))
            {
                throw new AnalyserException($"'{mnemonic}' instruction requires a register and a memory address operand", instruction.Span.Line, instruction.Span.StartColumn);
            }

            var memoryAddress = addressOperand.GetAddress();
            switch (memoryAddress)
            {
                case MemoryAddress.Immediate(var hexAddress):
                    var addressValue = OperandValueProcessor.ParseAddressValueString(hexAddress);
                    emitNode = new DataEmitNode([ GetOpcodeValue(registerOperand, opcode), ..addressValue ]);
                    labelRefNode = null;
                    break;
                case MemoryAddress.Label(var labelReference):
                    emitNode = new DataEmitNode([ GetOpcodeValue(registerOperand, opcode) ]);
                    labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReference);
                    break;
                case MemoryAddress.LabelWithPositiveOffset(var labelReference, var offset):
                    emitNode = new DataEmitNode([ GetOpcodeValue(registerOperand, opcode) ]);
                    var positiveOffset = OperandValueProcessor.ParseHexNumberString(offset.Value);
                    labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReference, positiveOffset);
                    break;
                case MemoryAddress.LabelWithNegativeOffset(var labelReference, var offset):
                    emitNode = new DataEmitNode([ GetOpcodeValue(registerOperand, opcode) ]);
                    var negativeOffset = OperandValueProcessor.ParseHexNumberString(offset.Value) * -1;
                    labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReference, negativeOffset);
                    break;
                default:
                    throw new AnalyserException($"Invalid memory address operand for '{mnemonic}' instruction", instruction.Span.Line, instruction.Span.StartColumn);
            }
        }
        public int Count => emitNode.Count + (labelRefNode?.Count ?? 0);
        public byte[] EmitBytes() => [..emitNode.Emit(), ..(labelRefNode?.Emit() ?? [])];

        private static byte GetOpcodeValue(RegisterNode registerNode, OpcodeBaseCode opcode)
        {
            var regIdx = Convert.ToByte(registerNode.RegisterName) & 0x03;
            return (byte)((byte)opcode | regIdx);
        }
        
        private readonly DataEmitNode emitNode;
        private readonly LabelReferenceEmitNode? labelRefNode;
    }
}
