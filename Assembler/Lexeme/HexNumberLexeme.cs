namespace Assembler.Lexeme
{
    [Lexeme(TokenType.HexNumber)]
    internal class HexNumberLexeme: ILexeme
    {
        public bool TryMatch(string source, int column, out string matchedText)
        {
            // Check for 0x prefix (at least 2 characters needed)
            if (column + 1 >= source.Length || 
                source[column] != '0' || 
                source[column + 1] != 'x')
            {
                matchedText = string.Empty;
                return false;
            }

            int startIdx = column + 2; // Skip past "0x"
            while (startIdx < source.Length && IsHexDigit(source[startIdx]))
            {
                startIdx++;
            }

            // Must have at least one hex digit after 0x
            if (startIdx > column + 2)
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
