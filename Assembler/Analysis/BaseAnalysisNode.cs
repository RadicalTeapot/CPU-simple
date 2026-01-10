using Assembler.Analysis.EmitNode;
using Assembler.AST;
using CPU.opcodes;

namespace Assembler.Analysis
{
    internal abstract class BaseAnalysisNode
    {
        public int Count => EmitNodes.Sum(node => node.Count);
        public IEmitNode[] EmitNodes { get; init; } = [];

        protected static byte GetOpcodeByte(OpcodeBaseCode opcode) => (byte)opcode;
        protected static byte GetOpcodeByteWithRegister(OpcodeBaseCode opcode, RegisterNode registerOperand)
        {
            var regIdx = Convert.ToByte(registerOperand.RegisterName) & 0x03;
            return (byte)((byte)opcode | regIdx);
        }
        protected static byte GetOpcodeByteWithTwoRegisters(OpcodeBaseCode opcode, RegisterNode firstRegisterOperand, RegisterNode secondRegisterOperand)
        {
            var firstRegIdx = Convert.ToByte(firstRegisterOperand.RegisterName) & 0x03;
            var secondRegIdx = Convert.ToByte(secondRegisterOperand.RegisterName) & 0x03;
            return (byte)((byte)opcode | (secondRegIdx << 2) | firstRegIdx);
        }
    }
}
