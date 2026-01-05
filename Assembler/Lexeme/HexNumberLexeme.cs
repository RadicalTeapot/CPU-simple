namespace Assembler.Lexeme
{
    [Lexeme(TokenType.HexNumber)]
    internal class HexNumberLexeme: ILexeme
    {
        public bool TryMatch(string source, int column, out string matchedText)
        {
            int startIdx = column;
            while (startIdx < source.Length && IsHexDigit(source[startIdx]))
            {
                startIdx++;
            }

            if (startIdx > column)
            {
                matchedText = source[column..startIdx];
                return true;
            }
            
            matchedText = string.Empty;
            return false;
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }
    }
}
