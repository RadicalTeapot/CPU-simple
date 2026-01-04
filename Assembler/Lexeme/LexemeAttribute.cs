using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class LexemeAttribute(
        TokenType type, 
        bool shouldFailIfAtEndOfLine
        ) : Attribute
    {
        public TokenType Type { get; } = type;
        public bool ShouldFailIfAtEndOfLine { get; } = shouldFailIfAtEndOfLine;
    }
}
