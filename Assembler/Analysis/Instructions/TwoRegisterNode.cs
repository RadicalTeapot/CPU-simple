using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class TwoRegisterNode : BaseAnalysisNode
    {
        public TwoRegisterNode(InstructionNode instruction, OpcodeBaseCode opcode)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.TwoRegistersOperand(var firstOperand, var secondOperand))
            {
                throw new AnalyserException($"'{mnemonic}' instruction requires two register operands", instruction.Span.Line, instruction.Span.StartColumn);
            }
            var opcodeByte = GetOpcodeByteWithTwoRegisters(opcode, firstOperand, secondOperand);
            EmitNodes = [new DataEmitNode([opcodeByte])];
        }
    }
}
