using System.Diagnostics;
using System.Reflection;

namespace Assembler.Lexeme
{
    public readonly struct Token(TokenType type, string lexeme, int line, int column)
    {
        public TokenType Type { get; } = type;
        public string Lexeme { get; } = lexeme;
        public int Line { get; } = line;
        public int Column { get; } = column;
        public int Length => Lexeme.Length;

        public override string ToString()
        {
            return $"{Type} '{Lexeme}' at ({Line}, {Column})";
        }
    }

    internal class TokenFactory
    {
        public TokenFactory()
        {
            lexemes = DiscoverLexemes();
        }

        public bool TryCreateToken(string source, int line, int column, int startCol, out Token token, out int newColumn)
        {
            Debug.Assert(column >= 0 && column < source.Length);
            foreach (var lexeme in lexemes)
            {
                if (lexeme.Lexeme.TryMatch(source, column, out var matchedText))
                {
                    var endIdx = column + matchedText.Length;
                    token = new Token(lexeme.Type, matchedText, line, column+startCol); // Offset start to match actual column in source code
                    newColumn = endIdx;
                    return true;
                }
            }

            token = default;
            newColumn = column;
            return false;
        }

        private class LexemeMetadata(ILexeme lexeme, TokenType type, int priority)
        {
            public ILexeme Lexeme { get; init; } = lexeme;
            public TokenType Type { get; init; } = type;
            public int Priority { get; init; } = priority;

        }

        private static List<LexemeMetadata> DiscoverLexemes()
        {
            var lexemeTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetCustomAttribute<LexemeAttribute>() != null && typeof(ILexeme).IsAssignableFrom(t));

            var lexemes = new List<LexemeMetadata>();
            foreach (var type in lexemeTypes)
            {
                var attribute = (LexemeAttribute)type.GetCustomAttributes(typeof(LexemeAttribute), false).First();
                if (Activator.CreateInstance(type) is ILexeme lexemeInstance)
                {
                    lexemes.Add(new LexemeMetadata(
                        lexemeInstance,
                        attribute.Type,
                        attribute.Priority));
                }
            }

            return [.. lexemes.OrderByDescending(t => t.Priority)];
        }

        private readonly List<LexemeMetadata> lexemes;
    }
}
