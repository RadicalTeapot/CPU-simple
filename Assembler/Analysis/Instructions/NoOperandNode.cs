using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class NoOperandNode : BaseAnalysisNode
    {
        public NoOperandNode(InstructionNode instruction, OpcodeBaseCode opcode)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.None)
            {
                throw new AnalyserException($"'{mnemonic}' instruction does not take any operands", instruction.Span.Line, instruction.Span.StartColumn);
            }

            EmitNodes = [new DataEmitNode([GetOpcodeByte(opcode)])];
        }
    }
}
