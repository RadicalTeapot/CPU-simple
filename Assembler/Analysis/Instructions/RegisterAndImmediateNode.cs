using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class RegisterAndImmediateNode : BaseAnalysisNode
    {
        public RegisterAndImmediateNode(InstructionNode instruction, OpcodeBaseCode opcode, LabelReferenceManager labelRefManager)
        {
            var operands = instruction.GetOperands();
            switch (operands)
            {
                case InstructionOperandSet.RegisterAndImmediateValueOperand(var registerOperand, var immediateOperand):
                    var immediateValue = OperandValueProcessor.ParseHexByteString(immediateOperand.Value);
                    EmitNodes = [new DataEmitNode([GetOpcodeByteWithRegister(opcode, registerOperand), immediateValue], instruction.Span)];
                    break;
                case InstructionOperandSet.RegisterAndLabelOperand(var registerOperand, var labelReferenceOperand):
                    var labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReferenceOperand);
                    EmitNodes = [
                        new DataEmitNode([GetOpcodeByteWithRegister(opcode, registerOperand)], NodeSpan.Exclude(instruction.Span, labelRefNode.Span)), 
                        labelRefNode
                    ];
                    break;
                default:
                    var mnemonic = instruction.Mnemonic;
                    throw new AnalyserException($"'{mnemonic}' instruction requires a register and an immediate hex number or label operand", instruction.Span.Line, instruction.Span.StartColumn);
            }
        }
    }
}