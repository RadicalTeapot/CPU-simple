using Assembler.Lexeme;

namespace Assembler.AST
{
    public class LabelNode(string label, NodeSpan span) : BaseNode(span)
    {
        public string Label { get; } = label;

        public const int TokenCount = 2;

        public static bool IsValidLabelAtIndex(IList<Token> tokens, int index)
        {
            var tokensRemaining = tokens.Count - index;
            return tokens[index].Type == TokenType.Identifier
                && tokensRemaining >= TokenCount
                && tokens[index + 1].Type == TokenType.Colon;
        }

        public static LabelNode CreateFromTokens(IList<Token> tokens, int index)
        {
            if (!IsValidLabelAtIndex(tokens, index))
            {
                throw new ParserException("Invalid label syntax.", tokens[index].Line, tokens[index].Column);
            }
            var labelToken = tokens[index];
            var colonToken = tokens[index + 1];
            var value = labelToken.Lexeme;
            var span = new NodeSpan(labelToken.Column, colonToken.Column + colonToken.Lexeme.Length, labelToken.Line);
            return new LabelNode(value, span);
        }
    }
}
