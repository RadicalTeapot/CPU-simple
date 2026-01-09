using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class NoOperandInstruction : IAnalysisNode
    {
        public NoOperandInstruction(InstructionNode instruction, OpcodeBaseCode opcode)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.None)
            {
                throw new AnalyserException($"'{mnemonic}' instruction does not take any operands", instruction.Span.Line, instruction.Span.StartColumn);
            }

            emitNode = new DataEmitNode([(byte)opcode]);
        }

        public int Count => SizeInBytes;
        public byte[] EmitBytes() => emitNode.Emit();

        private const int SizeInBytes = 1;
        private readonly DataEmitNode emitNode;
    }
}
