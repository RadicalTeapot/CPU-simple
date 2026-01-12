using System.Runtime.InteropServices;

namespace Assembler.Lexeme
{
    public enum TokenType
    {
        // Single character tokens
        LeftSquareBracket, RightSquareBracket, Comma, Dot, Colon, Plus, Minus, Hash,
        // Multi-character tokens
        HexNumber, Identifier, Register, String,
        // Special tokens
        EndOfLine, EndOfFile
    }

    /// <summary>
    /// Attribute to define lexeme properties for token classes.
    /// </summary>
    /// <param name="type">The type of the token.</param>
    /// <param name="priority">The priority of the token (higher values indicate higher priority).</param>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class LexemeAttribute(TokenType type, int priority = 0) : Attribute
    {
        public TokenType Type { get; } = type;
        public int Priority { get; } = priority;
    }
}
