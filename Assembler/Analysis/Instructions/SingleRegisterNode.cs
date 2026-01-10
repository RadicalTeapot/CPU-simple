using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class SingleRegisterNode : BaseAnalysisNode
    {
        public SingleRegisterNode(InstructionNode instruction, OpcodeBaseCode opcode)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.SingleRegisterOperand(var register))
            {
                throw new AnalyserException($"'{mnemonic}' instruction takes a single operand", instruction.Span.Line, instruction.Span.StartColumn);
            }
            var opcodeByte = GetOpcodeByteWithRegister(opcode, register);
            EmitNodes = [new DataEmitNode([opcodeByte])];
        }
    }
}
