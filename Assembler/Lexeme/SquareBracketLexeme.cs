namespace Assembler.Lexeme
{
    [Lexeme(TokenType.LeftSquareBracket)]
    internal class LeftSquareBracketLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            if (line[column] == '[')
            {
                matchedText = "[";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }

    [Lexeme(TokenType.RightSquareBracket)]
    internal class RightSquareBracketLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            if (line[column] == ']')
            {
                matchedText = "]";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }
}
