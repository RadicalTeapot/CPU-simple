namespace Assembler.Lexeme
{
    [Lexeme(TokenType.Dot, true)]
    internal class DotLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            if (line[column] == '.')
            {
                matchedText = ".";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }
}
