namespace Assembler.Lexeme
{
    [Lexeme(TokenType.Colon)]
    internal class ColonLexeme : ILexeme
    {
        public bool TryMatch(string source, int column, out string matchedText)
        {
            if (source[column] == ':')
            {
                matchedText = ":";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }
}
