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

        protected static byte GetRegisterIndex(RegisterNode registerOperand)
        {
            var regNum = registerOperand.RegisterName[1..]; // Trim the 'R' prefix
            var value = Convert.ToInt32(regNum);
            if (value < 0 || value > 3)
            {
                throw new AnalyserException($"Register '{registerOperand.RegisterName}' is out of range. Valid registers are R0 to R3.", registerOperand.Span.Line, registerOperand.Span.StartColumn);
            }
            return (byte)(value & 0x03);
        }

        protected static byte GetOpcodeByteWithRegister(OpcodeBaseCode opcode, RegisterNode registerOperand)
        {
            return (byte)((byte)opcode | GetRegisterIndex(registerOperand));
        }

        protected static byte GetOpcodeByteWithTwoRegisters(OpcodeBaseCode opcode, RegisterNode firstRegisterOperand, RegisterNode secondRegisterOperand)
        {
            var firstRegIdx = GetRegisterIndex(firstRegisterOperand);
            var secondRegIdx = GetRegisterIndex(secondRegisterOperand);
            return (byte)((byte)opcode | (secondRegIdx << 2) | firstRegIdx);
        }
    }
}
