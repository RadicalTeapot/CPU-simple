namespace Assembler.Lexeme
{
    [Lexeme(TokenType.Register, priority: 1)] // Registers have higher priority than identifiers
    internal class RegisterLexeme : ILexeme
    {
        public bool TryMatch(string line, int column, out string matchedText)
        {
            int startColumn = column;
            if (column + 1 < line.Length && IsValidRegisterStart(line[column]) && IsValidRegisterPart(line[column + 1]))
            {
                column += 2;
                while (column < line.Length && IsValidRegisterPart(line[column]))
                {
                    column++;
                }
                matchedText = line[startColumn..column];
                return true;
            }

            matchedText = string.Empty;
            return false;
        }

        private static bool IsValidRegisterStart(char c) => c == 'r';
        private static bool IsValidRegisterPart(char c) => char.IsDigit(c);
    }
}
