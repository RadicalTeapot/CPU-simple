using Assembler.Lexeme;

namespace Assembler.AST
{
    public enum OperandType
    {
        Register,
        Immediate,
        LabelReference,
        MemoryAddress,
        StringLiteral
    }

    public class InstructionNode(string mnemonic, int tokenCount, OperandType[] signature, NodeSpan span) : BaseNode(span)
    {
        public string Mnemonic { get; } = mnemonic;
        public int TokenCount { get; private set; } = tokenCount;

        public bool HasSignature(params OperandType[] operandTypes)
            => operandTypes.Length == _signature.Length && !_signature.Except(operandTypes).Any();

        public void GetOperands(out MemoryAddressNode memoryAddressOperand)
        {
            if (!HasSignature(OperandType.MemoryAddress) || _memoryAddressOperand == null)
            {
                throw new ParserException("Instruction does not have a memory address operand.", Span.Line, Span.StartColumn);
            }
            memoryAddressOperand = _memoryAddressOperand;
        }

        public void GetOperands(out RegisterNode registerOperand)
        {
            if (!HasSignature(OperandType.Register) || _registerOperands == null || _registerOperands.Count != 1)
            {
                throw new ParserException("Instruction does not have a single register operand.", Span.Line, Span.StartColumn);
            }
            registerOperand = _registerOperands[0];
        }

        public void GetOperands(out RegisterNode firstRegisterOperand, out HexNumberNode immediateOperand)
        {
            if (!HasSignature(OperandType.Register, OperandType.Immediate) || _registerOperands == null || _registerOperands.Count != 1 || _immediateOperand == null)
            {
                throw new ParserException("Instruction does not have a register and an immediate operand.", Span.Line, Span.StartColumn);
            }
            firstRegisterOperand = _registerOperands[0];
            immediateOperand = _immediateOperand;
        }

        public void GetOperands(out RegisterNode registerOperand, out LabelReferenceNode labelReferenceOperand)
        {
            if (!HasSignature(OperandType.Register, OperandType.LabelReference) || _registerOperands == null || _registerOperands.Count != 1 || _labelReferenceOperand == null)
            {
                throw new ParserException("Instruction does not have a register and a label reference operand.", Span.Line, Span.StartColumn);
            }
            registerOperand = _registerOperands[0];
            labelReferenceOperand = _labelReferenceOperand;
        }

        public void GetOperands(out RegisterNode registerOperand, out MemoryAddressNode memoryAddressOperand)
        {
            if (!HasSignature(OperandType.Register, OperandType.MemoryAddress) || _registerOperands == null || _registerOperands.Count != 1 || _memoryAddressOperand == null)
            {
                throw new ParserException("Instruction does not have a register and a memory address operand.", Span.Line, Span.StartColumn);
            }
            registerOperand = _registerOperands[0];
            memoryAddressOperand = _memoryAddressOperand;
        }

        public void GetOperands(out RegisterNode firstRegisterOperand, out RegisterNode secondRegisterOperand)
        {
            if (!HasSignature(OperandType.Register, OperandType.Register) || _registerOperands == null || _registerOperands.Count != 2)
            {
                throw new ParserException("Instruction does not have two register operands.", Span.Line, Span.StartColumn);
            }
            firstRegisterOperand = _registerOperands[0];
            secondRegisterOperand = _registerOperands[1];
        }

        public static bool IsValidInstructionAtIndex(IList<Token> tokens, int index)
        {
            return tokens[index].Type == TokenType.Identifier;
        }

        public static InstructionNode CreateFromTokens(IList<Token> tokens, int index)
        {
            if (!IsValidInstructionAtIndex(tokens, index))
            {
                throw new ParserException("Invalid instruction syntax.", tokens[index].Line, tokens[index].Column);
            }

            InstructionNode instructionNode;

            var instructionToken = tokens[index];
            var mnemonic = instructionToken.Lexeme;
            index++;

            if (MemoryAddressNode.IsValidMemoryAddressAtIndex(tokens, index))
            {
                var memoryAddressOperand = MemoryAddressNode.CreateFromTokens(tokens, index);
                var span = new NodeSpan(instructionToken.Column, memoryAddressOperand.Span.EndColumn, instructionToken.Line);
                var tokenCount = memoryAddressOperand.TokenCount + 1; // +1 for the instruction token
                var signature = new OperandType[] { OperandType.MemoryAddress };
                instructionNode = new InstructionNode(mnemonic, tokenCount, signature, span)
                {
                    _memoryAddressOperand = memoryAddressOperand,
                };
            }
            else if (RegisterNode.IsValidRegisterNodeAtIndex(tokens, index))
            {
                var registerOperands = new List<RegisterNode>();
                var registerOperand = RegisterNode.CreateFromTokens(tokens, index);
                registerOperands.Add(registerOperand);

                var tokenCount = RegisterNode.TokenCount + 1; // +1 for the instruction token
                index += RegisterNode.TokenCount;

                HexNumberNode? immediateOperand = null;
                LabelReferenceNode? labelReferenceOperand = null;
                MemoryAddressNode? memoryAddressOperand = null;
                var signature = new List<OperandType> { OperandType.Register };
                if (HexNumberNode.IsValidHexNodeAtIndex(tokens, index))
                {
                    immediateOperand = HexNumberNode.CreateFromTokens(tokens, index);
                    tokenCount += HexNumberNode.TokenCount;
                    index += HexNumberNode.TokenCount;
                    signature.Add(OperandType.Immediate);
                }
                else if (LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, index))
                {
                    labelReferenceOperand = LabelReferenceNode.CreateFromTokens(tokens, index);
                    tokenCount += LabelReferenceNode.TokenCount;
                    index += LabelReferenceNode.TokenCount;
                    signature.Add(OperandType.LabelReference);
                }
                else if (MemoryAddressNode.IsValidMemoryAddressAtIndex(tokens, index))
                {
                    memoryAddressOperand = MemoryAddressNode.CreateFromTokens(tokens, index);
                    tokenCount += memoryAddressOperand.TokenCount;
                    index += memoryAddressOperand.TokenCount;
                    signature.Add(OperandType.MemoryAddress);
                }
                else if (RegisterNode.IsValidRegisterNodeAtIndex(tokens, index))
                {
                    var secondRegisterOperand = RegisterNode.CreateFromTokens(tokens, index);
                    registerOperands.Add(secondRegisterOperand);
                    tokenCount += RegisterNode.TokenCount;
                    index += RegisterNode.TokenCount;
                    signature.Add(OperandType.Register);
                }

                if (!(tokens[index].Type == TokenType.RightSquareBracket))
                {
                    throw new ParserException("Unexpected token after register operand(s).", tokens[index].Line, tokens[index].Column);
                }

                var span = new NodeSpan(instructionToken.Column, tokens[index].Column + tokens[index].Lexeme.Length, instructionToken.Line);
                instructionNode = new InstructionNode(mnemonic, tokenCount, [..signature], span)
                {
                    _registerOperands = registerOperands,
                    _immediateOperand = immediateOperand,
                    _labelReferenceOperand = labelReferenceOperand,
                    _memoryAddressOperand = memoryAddressOperand,
                };
            }
            else
            {
                var span = new NodeSpan(instructionToken.Column, instructionToken.Column + instructionToken.Lexeme.Length, instructionToken.Line);
                instructionNode = new InstructionNode(mnemonic, 1, [], span);
            }

            return instructionNode;
        }

        private List<RegisterNode>? _registerOperands;
        private HexNumberNode? _immediateOperand;
        private LabelReferenceNode? _labelReferenceOperand;
        private MemoryAddressNode? _memoryAddressOperand;

        private readonly OperandType[] _signature = signature;
    }
}