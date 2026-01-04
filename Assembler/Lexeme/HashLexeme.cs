namespace Assembler.Lexeme
{
    [Lexeme(TokenType.Hash, true)]
    internal class HashLexeme : ILexeme
    {
        public bool TryMatch(string source, int column, out string matchedText)
        {
            if (source[column] == '#')
            {
                matchedText = "#";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }
}
