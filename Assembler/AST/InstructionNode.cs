using Assembler.Lexeme;
using System.Diagnostics;

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

    public abstract record InstructionOperandSet
    {
        public record None : InstructionOperandSet;
        public record SingleMemoryAddressOperand(MemoryAddressNode Operand) : InstructionOperandSet;
        public record SingleRegisterOperand(RegisterNode Operand) : InstructionOperandSet;
        public record RegisterAndImmediateValueOperand(RegisterNode FirstOperand, ImmediateValueNode SecondOperand) : InstructionOperandSet;
        public record RegisterAndLabelOperand(RegisterNode FirstOperand, LabelReferenceNode SecondOperand) : InstructionOperandSet;
        public record RegisterAndMemoryAddressOperand(RegisterNode FirstOperand, MemoryAddressNode SecondOperand) : InstructionOperandSet;
        public record TwoRegistersOperand(RegisterNode FirstOperand, RegisterNode SecondOperand) : InstructionOperandSet;
    }

    public class InstructionNode(string mnemonic, int tokenCount, OperandType[] signature, NodeSpan span) : BaseNode(span)
    {
        public string Mnemonic { get; } = mnemonic;
        public int TokenCount { get; private set; } = tokenCount;

        public InstructionOperandSet GetOperands()
        {
            switch (_signature)
            {
                case []: return new InstructionOperandSet.None();
                case [OperandType.MemoryAddress]:
                    Debug.Assert(_memoryAddressOperand != null, "Instruction has no single memory address operand");
                    return new InstructionOperandSet.SingleMemoryAddressOperand(_memoryAddressOperand);
                case [OperandType.Register]:
                    if (_registerOperands == null || _registerOperands.Count != 1)
                    Debug.Assert(_registerOperands != null && _registerOperands.Count == 1, "Instruction has no single register operand");
                    return new InstructionOperandSet.SingleRegisterOperand(_registerOperands[0]);
                case [OperandType.Register, OperandType.Immediate]:
                    Debug.Assert(_registerOperands != null && _registerOperands.Count == 1 && _immediateOperand != null, "Instruction has no register and immediate operand");
                    return new InstructionOperandSet.RegisterAndImmediateValueOperand(_registerOperands[0], _immediateOperand);
                case [OperandType.Register, OperandType.LabelReference]:
                    Debug.Assert(_registerOperands != null && _registerOperands.Count == 1 && _labelReferenceOperand != null, "Instruction has no register and label reference operand");
                    return new InstructionOperandSet.RegisterAndLabelOperand(_registerOperands[0], _labelReferenceOperand);
                case [OperandType.Register, OperandType.MemoryAddress]:
                    Debug.Assert(_registerOperands != null && _registerOperands.Count == 1 && _memoryAddressOperand != null, "Instruction has no register and memory address operand");
                    return new InstructionOperandSet.RegisterAndMemoryAddressOperand(_registerOperands[0], _memoryAddressOperand);
                case [OperandType.Register, OperandType.Register]:
                    Debug.Assert(_registerOperands != null && _registerOperands.Count == 2, "Instruction does not have two register operands");
                    return new InstructionOperandSet.TwoRegistersOperand(_registerOperands[0], _registerOperands[1]);
                default:
                    throw new ParserException("Unkown signature for instruction operands", Span.Line, Span.StartColumn);
            }
            ;
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

            if (tokens.Count > index && MemoryAddressNode.IsValidMemoryAddressAtIndex(tokens, index))
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
            else if (tokens.Count > index && RegisterNode.IsValidRegisterNodeAtIndex(tokens, index))
            {
                var registerOperands = new List<RegisterNode>();
                var registerOperand = RegisterNode.CreateFromTokens(tokens, index);
                registerOperands.Add(registerOperand);

                var tokenCount = RegisterNode.TokenCount + 1; // +1 for the instruction token
                index += RegisterNode.TokenCount;

                ImmediateValueNode? immediateOperand = null;
                LabelReferenceNode? labelReferenceOperand = null;
                MemoryAddressNode? memoryAddressOperand = null;
                var signature = new List<OperandType> { OperandType.Register };

                if (tokens.Count > index && tokens[index].Type == TokenType.Comma)
                {
                    index++; // Skip comma
                    tokenCount++; // for the comma

                    if (tokens.Count > index && ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, index))
                    {
                        immediateOperand = ImmediateValueNode.CreateFromTokens(tokens, index);
                        tokenCount += ImmediateValueNode.TokenCount;
                        index += ImmediateValueNode.TokenCount;
                        signature.Add(OperandType.Immediate);
                    }
                    else if (tokens.Count > index && LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, index))
                    {
                        labelReferenceOperand = LabelReferenceNode.CreateFromTokens(tokens, index);
                        tokenCount += LabelReferenceNode.TokenCount;
                        index += LabelReferenceNode.TokenCount;
                        signature.Add(OperandType.LabelReference);
                    }
                    else if (tokens.Count > index && MemoryAddressNode.IsValidMemoryAddressAtIndex(tokens, index))
                    {
                        memoryAddressOperand = MemoryAddressNode.CreateFromTokens(tokens, index);
                        tokenCount += memoryAddressOperand.TokenCount;
                        index += memoryAddressOperand.TokenCount;
                        signature.Add(OperandType.MemoryAddress);
                    }
                    else if (tokens.Count > index && RegisterNode.IsValidRegisterNodeAtIndex(tokens, index))
                    {
                        var secondRegisterOperand = RegisterNode.CreateFromTokens(tokens, index);
                        registerOperands.Add(secondRegisterOperand);
                        tokenCount += RegisterNode.TokenCount;
                        index += RegisterNode.TokenCount;
                        signature.Add(OperandType.Register);
                    }
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
            else if (tokens.Count > index && tokens[index].Type == TokenType.EndOfLine)
            {
                var span = new NodeSpan(instructionToken.Column, instructionToken.Column + instructionToken.Lexeme.Length, instructionToken.Line);
                instructionNode = new InstructionNode(mnemonic, 1, [], span);
            }
            else
            {
                throw new ParserException("Invalid instruction operands.", tokens[index].Line, tokens[index].Column);
            }

            return instructionNode;
        }

        private List<RegisterNode>? _registerOperands;
        private ImmediateValueNode? _immediateOperand;
        private LabelReferenceNode? _labelReferenceOperand;
        private MemoryAddressNode? _memoryAddressOperand;

        private readonly OperandType[] _signature = signature;
    }
}