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
                while (column < line.Length)
                {
                    // Handle escape sequences: \\ and \"
                    if (line[column] == '\\' && column + 1 < line.Length)
                    {
                        var nextChar = line[column + 1];
                        if (nextChar == '\\' || nextChar == '"')
                        {
                            column += 2; // Skip the escape sequence
                            continue;
                        }
                    }

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
