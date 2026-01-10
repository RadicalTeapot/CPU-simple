using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class RegisterAndMemoryAddressNode : BaseAnalysisNode
    {
        public RegisterAndMemoryAddressNode(InstructionNode instruction, OpcodeBaseCode opcode, LabelReferenceManager labelRefManager)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.RegisterAndMemoryAddressOperand(var registerOperand, var addressOperand))
            {
                throw new AnalyserException($"'{mnemonic}' instruction requires a register and a memory address operand", instruction.Span.Line, instruction.Span.StartColumn);
            }

            var opcodeByte = GetOpcodeByteWithRegister(opcode, registerOperand);
            var memoryAddress = addressOperand.GetAddress();
            switch (memoryAddress)
            {
                case MemoryAddress.Immediate(var hexAddress):
                    var addressValue = OperandValueProcessor.ParseAddressValueString(hexAddress);
                    EmitNodes = [
                        new DataEmitNode([opcodeByte, ..addressValue])
                    ];
                    break;
                case MemoryAddress.Label(var labelReference):
                    EmitNodes = [
                        new DataEmitNode([opcodeByte]),
                        labelRefManager.CreateAndRegisterEmitNode(labelReference)
                    ];
                    break;
                case MemoryAddress.LabelWithPositiveOffset(var labelReference, var offset):
                    var positiveOffset = OperandValueProcessor.ParseHexNumberString(offset.Value);
                    EmitNodes = [
                        new DataEmitNode([opcodeByte]),
                        labelRefManager.CreateAndRegisterEmitNode(labelReference, positiveOffset)
                    ];
                    break;
                case MemoryAddress.LabelWithNegativeOffset(var labelReference, var offset):
                    var negativeOffset = OperandValueProcessor.ParseHexNumberString(offset.Value) * -1;
                    EmitNodes = [
                        new DataEmitNode([opcodeByte]),
                        labelRefManager.CreateAndRegisterEmitNode(labelReference, negativeOffset)
                    ];
                    break;
                default:
                    throw new AnalyserException($"Invalid memory address operand for '{mnemonic}' instruction", instruction.Span.Line, instruction.Span.StartColumn);
            }
        }
    }
}
