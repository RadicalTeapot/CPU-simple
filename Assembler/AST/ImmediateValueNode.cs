using Assembler.Lexeme;

namespace Assembler.AST
{
    public class ImmediateValueNode(string value, NodeSpan span) : BaseNode(span)
    {
        public string Value { get; } = value;

        public const int TokenCount = 2;

        public static bool IsValidImmediateValueNodeAtIndex(IList<Token> tokens, int index)
        {
            if (tokens[index].Type != TokenType.Hash)
            {
                return false;
            }

            var tokensRemaining = tokens.Count - index;
            if (tokensRemaining < TokenCount                        // Not enough tokens
                || tokens[index + 1].Type != TokenType.HexNumber)   // Next token is not a hex number
            {
                throw new ParserException("Invalid immediate value syntax.", tokens[index].Line, tokens[index].Column);
            }

            return true;
        }

        public static ImmediateValueNode CreateFromTokens(IList<Token> tokens, int index)
        {
            if (!IsValidImmediateValueNodeAtIndex(tokens, index))
            {
                throw new ParserException("Invalid hex number operand syntax.", tokens[index].Line, tokens[index].Column);
            }

            var hashToken = tokens[index];
            var hexNumberToken = tokens[index + 1];
            var value = hexNumberToken.Lexeme;
            var span = new NodeSpan(hashToken.Column, hexNumberToken.Column + hexNumberToken.Lexeme.Length, hashToken.Line);
            return new ImmediateValueNode(value, span);
        }
    }
}
