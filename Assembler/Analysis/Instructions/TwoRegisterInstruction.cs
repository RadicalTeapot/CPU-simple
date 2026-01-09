using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class TwoRegisterInstruction : IAnalysisNode
    {
        public TwoRegisterInstruction(InstructionNode instruction, OpcodeBaseCode opcode)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.TwoRegistersOperand(var firstOperand, var secondOperand))
            {
                throw new AnalyserException($"'{mnemonic}' instruction requires two register operands", instruction.Span.Line, instruction.Span.StartColumn);
            }
            var destReg = Convert.ToByte(firstOperand.RegisterName) & 0x03;
            var srcReg = Convert.ToByte(secondOperand.RegisterName) & 0x03;
            var opcodeValue = (byte)((byte)opcode | (srcReg << 2) | destReg);
            emitNode = new DataEmitNode([opcodeValue]);
        }

        public int Count => emitNode.Count;
        public byte[] EmitBytes() => emitNode.Emit();

        private readonly DataEmitNode emitNode;
    }
}
