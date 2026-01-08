namespace Assembler.AST
{
    public class LabelReferenceNode(string labelName, NodeSpan span) : BaseNode(span)
    {
        public string Label { get; } = labelName;

        public const int TokenCount = 1;

        public static bool IsValidLabelReferenceAtIndex(IList<Lexeme.Token> tokens, int index)
        {
            return tokens[index].Type == Lexeme.TokenType.Identifier;
        }

        public static LabelReferenceNode CreateFromTokens(IList<Lexeme.Token> tokens, int index)
        {
            if (!IsValidLabelReferenceAtIndex(tokens, index))
            {
                throw new ParserException("Invalid label reference syntax.", tokens[index].Line, tokens[index].Column);
            }
            var labelToken = tokens[index];
            var value = labelToken.Lexeme;
            var span = new NodeSpan(labelToken.Column, labelToken.Column + labelToken.Lexeme.Length, labelToken.Line);
            return new LabelReferenceNode(value, span);
        }
    }
}
