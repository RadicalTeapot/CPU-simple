namespace Assembler.Lexeme
{
    public enum TokenType
    {
        // Single character tokens
        LeftSquareBracket, RightSquareBracket, Comma, Dot, Colon, Plus, Minus, Hash,
        // Multi-character tokens
        HexNumber, Identifier, Register, String,
    }

    public readonly struct Token(TokenType type, string lexeme, int line, int column)
    {
        public TokenType Type { get; init; } = type;
        public string Lexeme { get; init; } = lexeme;
        public int Line { get; init; } = line;
        public int Column { get; init; } = column;

        public override string ToString()
        {
            return $"{Type} '{Lexeme}' at ({Line}, {Column})";
        }
    }

    /// <summary>
    /// Attribute to define lexeme properties for token classes.
    /// </summary>
    /// <param name="type">The type of the token.</param>
    /// <param name="shouldFailIfAtEndOfLine">Indicates if the lexer should fail if the token is at the end of the line.</param>
    /// <param name="priority">The priority of the token (higher values indicate higher priority).</param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class LexemeAttribute(
        TokenType type, 
        bool shouldFailIfAtEndOfLine = false,
        int priority = 0
        ) : Attribute
    {
        public TokenType Type { get; } = type;
        public bool ShouldFailIfAtEndOfLine { get; } = shouldFailIfAtEndOfLine;
        public int Priority { get; } = priority;
    }
}
