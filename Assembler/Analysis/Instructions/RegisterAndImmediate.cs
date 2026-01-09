using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class RegisterAndImmediate : IAnalysisNode
    {
        public RegisterAndImmediate(InstructionNode instruction, OpcodeBaseCode opcode, LabelReferenceManager labelRefManager)
        {
            var operands = instruction.GetOperands();
            switch (operands)
            {
                case InstructionOperandSet.RegisterAndHexNumberOperand(var registerOperand, var immediateOperand):
                    var immediateValue = OperandValueProcessor.ParseHexByteString(immediateOperand.Value);
                    emitNode = new DataEmitNode([GetOpcodeValue(registerOperand, opcode), immediateValue]);
                    labelRefNode = null;
                    break;
                case InstructionOperandSet.RegisterAndLabelOperand(var registerOperand, var labelReferenceOperand):
                    emitNode = new DataEmitNode([GetOpcodeValue(registerOperand, opcode)]);
                    labelRefNode = labelRefManager.CreateAndRegisterEmitNode(labelReferenceOperand);
                    break;
                default:
                    var mnemonic = instruction.Mnemonic;
                    throw new AnalyserException($"'{mnemonic}' instruction requires a register and an immediate hex number or label operand", instruction.Span.Line, instruction.Span.StartColumn);
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