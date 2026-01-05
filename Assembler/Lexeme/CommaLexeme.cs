namespace Assembler.Lexeme
{
    [Lexeme(TokenType.Comma, shouldFailIfAtEndOfLine: true)]
    internal class CommaLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            if (line[column] == ',')
            {
                matchedText = ",";
                return true;
            }

            matchedText = string.Empty;
            return false;
        }
    }
}
