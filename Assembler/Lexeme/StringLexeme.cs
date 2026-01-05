namespace Assembler.Lexeme
{
    [Lexeme(TokenType.String)]
    internal class StringLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            int startColumn = column;
            if (column < line.Length && line[column] == '"')
            {
                column++;
                while (column < line.Length) // TODO : Change this to handle only ASCII characters and escape sequences \0 and \"
                {
                    if (line[column] == '"')
                    {
                        column++; // Include the closing quote
                        matchedText = line[startColumn..column];
                        return true;
                    }
                    column++;
                }
            }
            matchedText = string.Empty;
            return false;
        }
    }
}
