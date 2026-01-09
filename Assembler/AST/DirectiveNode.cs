using Assembler.Lexeme;

namespace Assembler.AST
{
    public class DirectiveNode(string directive, int tokenCount, NodeSpan span) : BaseNode(span)
    {
        public string Directive { get; } = directive;
        public int TokenCount { get; } = tokenCount;

        public bool HasSignature(params OperandType[] operandTypes)
            => operandTypes.Length == _signature.Length && !_signature.SequenceEqual(operandTypes); // SequenceEqual also check for correct order

        public void GetOperands(out StringLiteralNode stringOperand)
        {
            if (!HasSignature(OperandType.StringLiteral) || _stringOperand == null)
            {
                throw new ParserException("Directive does not have a string operand.", Span.Line, Span.StartColumn);
            }
            stringOperand = _stringOperand;
        }

        public void GetOperands(out HexNumberNode hexOperand)
        {
            if (!HasSignature(OperandType.Immediate) || _hexNumberOperands == null || _hexNumberOperands.Count != 1)
            {
                throw new ParserException("Directive does not have a single hex number operand.", Span.Line, Span.StartColumn);
            }
            hexOperand = _hexNumberOperands[0];
        }

        public void GetOperands(out HexNumberNode firstHexOperand, out HexNumberNode secondHexOperand)
        {
            if (!HasSignature(OperandType.Immediate, OperandType.Immediate) || _hexNumberOperands == null || _hexNumberOperands.Count != 2)
            {
                throw new ParserException("Directive does not have two hex number operands.", Span.Line, Span.StartColumn);
            }
            firstHexOperand = _hexNumberOperands[0];
            secondHexOperand = _hexNumberOperands[1];
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

            var immediateOperands = new List<HexNumberNode>();
            if (HexNumberNode.IsValidHexNodeAtIndex(tokens, currentTokenIndex))
            {
                immediateOperands.Add(HexNumberNode.CreateFromTokens(tokens, currentTokenIndex));
                currentTokenIndex += HexNumberNode.TokenCount;

                // Check for a comma, indicating a second operand
                if (tokens[currentTokenIndex].Type == TokenType.Comma)
                {
                    currentTokenIndex++;
                    if (HexNumberNode.IsValidHexNodeAtIndex(tokens, currentTokenIndex))
                    {
                        immediateOperands.Add(HexNumberNode.CreateFromTokens(tokens, currentTokenIndex));
                        currentTokenIndex += HexNumberNode.TokenCount;
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
                    _hexNumberOperands = immediateOperands,
                    _signature = signature
                };
            }
            else if (StringLiteralNode.IsValidStringOperandNodeAtIndex(tokens, currentTokenIndex))
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
        private List<HexNumberNode>? _hexNumberOperands;
        private OperandType[] _signature = [];
        private const int minTokenCount = 2; // .directive
    }
}
