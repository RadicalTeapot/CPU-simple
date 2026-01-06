namespace Assembler.Lexeme
{
    [Lexeme(TokenType.Identifier)]
    internal class IdentifierLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            int startColumn = column;
            if (column < line.Length && IsValidStartChar(line[column]))
            {
                column++;
                while (column < line.Length && IsValidPartChar(line[column]))
                {
                    column++;
                }
                matchedText = line[startColumn..column];
                return true;
            }
            matchedText = string.Empty;
            return false;
        }

        private static bool IsValidStartChar(char c) => char.IsLetter(c) || c == '_';
        private static bool IsValidPartChar(char c) => char.IsLetterOrDigit(c) || c == '_';
    }
}
