using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class RegisterAndMemoryAddressNode : BaseAnalysisNode
    {
        public RegisterAndMemoryAddressNode(
            InstructionNode instruction, OpcodeBaseCode opcode, LabelReferenceManager labelRefManager,
            MemoryAddressValueProcessor memoryAddressValueProcessor)
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
                    var addressValue = memoryAddressValueProcessor.ParseAddressValueStringAsByteArray(hexAddress);
                    EmitNodes = [
                        new DataEmitNode([opcodeByte, ..addressValue], instruction.Span)
                    ];
                    break;
                case MemoryAddress.Label(var labelReference):
                    EmitNodes = [
                        new DataEmitNode([opcodeByte], instruction.Span),
                        labelRefManager.CreateAndRegisterEmitNode(labelReference)
                    ];
                    break;
                case MemoryAddress.LabelWithPositiveOffset(var labelReference, var offset):
                    var positiveOffset = OperandValueProcessor.ParseHexNumberString(offset.Value);
                    EmitNodes = [
                        new DataEmitNode([opcodeByte], instruction.Span),
                        labelRefManager.CreateAndRegisterEmitNode(labelReference, positiveOffset)
                    ];
                    break;
                case MemoryAddress.LabelWithNegativeOffset(var labelReference, var offset):
                    var negativeOffset = OperandValueProcessor.ParseHexNumberString(offset.Value) * -1;
                    EmitNodes = [
                        new DataEmitNode([opcodeByte], instruction.Span),
                        labelRefManager.CreateAndRegisterEmitNode(labelReference, negativeOffset)
                    ];
                    break;
                case MemoryAddress.Register(var registerAddress):
                    EmitNodes = [
                        new DataEmitNode([opcodeByte, GetRegisterIndex(registerAddress)], instruction.Span)
                    ];
                    break;
                case MemoryAddress.RegisterWithPositiveOffset(var registerAddress, var offset):
                    var regPositiveOffset = OperandValueProcessor.ParseHexNumberString(offset.Value);
                    if (regPositiveOffset > 0x3F)
                    {
                        throw new AnalyserException($"Offset for '{mnemonic}' instruction must be smaller than 64", instruction.Span.Line, instruction.Span.StartColumn);
                    }
                    EmitNodes = [
                        new DataEmitNode([opcodeByte, (byte)(regPositiveOffset << 2 | GetRegisterIndex(registerAddress))], instruction.Span)
                    ];
                    break;
                default:
                    throw new AnalyserException($"Invalid memory address operand for '{mnemonic}' instruction", instruction.Span.Line, instruction.Span.StartColumn);
            }
        }
    }
}
