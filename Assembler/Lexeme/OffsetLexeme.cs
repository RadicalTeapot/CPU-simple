namespace Assembler.Lexeme
{
    [Lexeme(TokenType.Plus, shouldFailIfAtEndOfLine: true)]
    internal class PositiveOffsetLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            if (line[column] == '+')
            {
                matchedText = "+";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }

    [Lexeme(TokenType.Minus, true)]
    internal class NegativeOffsetLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            if (line[column] == '-')
            {
                matchedText = "-";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }
}
