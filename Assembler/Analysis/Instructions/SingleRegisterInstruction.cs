using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis.Instructions
{
    internal class SingleRegisterInstruction : IAnalysisNode
    {
        public SingleRegisterInstruction(InstructionNode instruction, OpcodeBaseCode opcode)
        {
            var mnemonic = instruction.Mnemonic;
            var operands = instruction.GetOperands();
            if (operands is not InstructionOperandSet.SingleRegisterOperand(var register))
            {
                throw new AnalyserException($"'{mnemonic}' instruction takes a single operand", instruction.Span.Line, instruction.Span.StartColumn);
            }
            var reg = Convert.ToByte(register.RegisterName) & 0x03;
            var opcodeValue = (byte)((byte)opcode | reg);
            emitNode = new DataEmitNode([opcodeValue]);
        }

        public int Count => emitNode.Count;
        public byte[] EmitBytes() => emitNode.Emit();

        private readonly DataEmitNode emitNode;
    }
}
