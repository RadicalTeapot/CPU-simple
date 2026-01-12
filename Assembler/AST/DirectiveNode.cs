using Assembler.Lexeme;
using System.Diagnostics;

namespace Assembler.AST
{
    public abstract record DirectiveOperandSet
    {
        public record None : DirectiveOperandSet;
        public record SingleStringOperand(StringLiteralNode Operand) : DirectiveOperandSet;
        public record SingleHexNumberOperand(ImmediateValueNode Operand) : DirectiveOperandSet;
        public record PairOfImmediateValueOperands(ImmediateValueNode FirstOperand, ImmediateValueNode SecondOperand) : DirectiveOperandSet;
    }

    public class DirectiveNode(string directive, int tokenCount, NodeSpan span) : BaseNode(span)
    {
        public string Directive { get; } = directive;
        public int TokenCount { get; } = tokenCount;

        public DirectiveOperandSet GetOperands()
        {
            switch(_signature) 
            {
                case []: return new DirectiveOperandSet.None();
                case [OperandType.StringLiteral]:
                    Debug.Assert(_stringOperand != null, "Directive has no single string operand");
                    return new DirectiveOperandSet.SingleStringOperand(_stringOperand);
                case [OperandType.Immediate]:
                    Debug.Assert(_immediateOperands != null && _immediateOperands.Count == 1, "Directive has no single immediate operand");
                    return new DirectiveOperandSet.SingleHexNumberOperand(_immediateOperands[0]);
                case [OperandType.Immediate, OperandType.Immediate]:
                    Debug.Assert(_immediateOperands != null && _immediateOperands.Count == 2, "Directive does not have two immediate operands");
                    return new DirectiveOperandSet.PairOfImmediateValueOperands(_immediateOperands[0], _immediateOperands[1]);
                default:
                    throw new ParserException("Unkown signature for directive operands", Span.Line, Span.StartColumn);
            };
        }

        public static bool IsValidDirectiveAtIndex(IList<Token> tokens, int index)
        {
            if (tokens[index].Type != TokenType.Dot)
            {
                return false;
            }

            var tokensRemaining = tokens.Count - index;
            if (tokensRemaining < minTokenCount                     // Not enough tokens
                || tokens[index + 1].Type != TokenType.Identifier)  // Next token is not an identifier
            {
                throw new ParserException("Invalid directive syntax.", tokens[index].Line, tokens[index].Column);
            }

            // Further validation will be done in CreateFromTokens
            return true;
        }

        public static DirectiveNode CreateFromTokens(IList<Token> tokens, int index)
        {
            if (!IsValidDirectiveAtIndex(tokens, index))
            {
                throw new ParserException("Invalid directive syntax.", tokens[index].Line, tokens[index].Column);
            }

            var startIndex = index;
            var directiveNameToken = tokens[index + 1];
            var currentTokenIndex = index + 2; // Move past .directive

            var immediateOperands = new List<ImmediateValueNode>();
            if (tokens.Count > currentTokenIndex && ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, currentTokenIndex))
            {
                immediateOperands.Add(ImmediateValueNode.CreateFromTokens(tokens, currentTokenIndex));
                currentTokenIndex += ImmediateValueNode.TokenCount;

                // Check for a comma, indicating a second operand
                if (tokens.Count > currentTokenIndex && tokens[currentTokenIndex].Type == TokenType.Comma)
                {
                    currentTokenIndex++;
                    if (tokens.Count > currentTokenIndex && ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, currentTokenIndex))
                    {
                        immediateOperands.Add(ImmediateValueNode.CreateFromTokens(tokens, currentTokenIndex));
                        currentTokenIndex += ImmediateValueNode.TokenCount;
                    }
                    else
                    {
                        throw new ParserException("Expected second operand after comma.", tokens[currentTokenIndex].Line, tokens[currentTokenIndex].Column);
                    }
                }
            }

            var tokenCount = currentTokenIndex - startIndex;
            DirectiveNode directiveNode;
            if (immediateOperands.Count > 0)
            {
                var nodeSpan = new NodeSpan(
                    tokens[startIndex].Column, immediateOperands.Last().Span.EndColumn, tokens[startIndex].Line);

                var signature = new OperandType[immediateOperands.Count];
                Array.Fill(signature, OperandType.Immediate);

                directiveNode = new DirectiveNode(directiveNameToken.Lexeme, tokenCount, nodeSpan)
                {
                    _immediateOperands = immediateOperands,
                    _signature = signature
                };
            }
            else if (tokens.Count > currentTokenIndex && StringLiteralNode.IsValidStringOperandNodeAtIndex(tokens, currentTokenIndex))
            {
                var stringOperandToken = StringLiteralNode.CreateFromTokens(tokens, currentTokenIndex);
                var nodeSpan = new NodeSpan(
                    tokens[startIndex].Column, stringOperandToken.Span.EndColumn, tokens[startIndex].Line);
                directiveNode = new DirectiveNode(directiveNameToken.Lexeme, tokenCount + StringLiteralNode.TokenCount, nodeSpan)
                {
                    _stringOperand = stringOperandToken,
                    _signature = [OperandType.StringLiteral]
                };
            }
            else
            {
                var nodeSpan = new NodeSpan(
                    tokens[startIndex].Column, directiveNameToken.Column + directiveNameToken.Lexeme.Length, tokens[startIndex].Line);
                directiveNode = new DirectiveNode(directiveNameToken.Lexeme, tokenCount, nodeSpan);
            }

            return directiveNode;
        }

        private StringLiteralNode? _stringOperand;
        private List<ImmediateValueNode>? _immediateOperands;
        private OperandType[] _signature = [];
        private const int minTokenCount = 2; // .directive
    }
}
