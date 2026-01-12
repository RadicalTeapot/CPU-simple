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
            var regNum = registerOperand.RegisterName[1..]; // Trim the 'R' prefix
            var regIdx = Convert.ToByte(regNum) & 0x03;
            return (byte)((byte)opcode | regIdx);
        }

        protected static byte GetOpcodeByteWithTwoRegisters(OpcodeBaseCode opcode, RegisterNode firstRegisterOperand, RegisterNode secondRegisterOperand)
        {
            var firstRegNum = firstRegisterOperand.RegisterName[1..]; // Trim the 'R' prefix
            var firstRegIdx = Convert.ToByte(firstRegNum) & 0x03;
            var secondRegNum = secondRegisterOperand.RegisterName[1..]; // Trim the 'R' prefix
            var secondRegIdx = Convert.ToByte(secondRegNum) & 0x03;
            return (byte)((byte)opcode | (secondRegIdx << 2) | firstRegIdx);
        }
    }
}
