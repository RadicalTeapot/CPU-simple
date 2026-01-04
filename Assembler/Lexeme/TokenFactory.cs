using System.Reflection;

namespace Assembler.Lexeme
{
    internal class TokenFactory
    {
        public TokenFactory()
        {
            lexemes = DiscoverLexemes();
        }

        public bool TryCreateToken(string source, int line, int column, out Token token, out int newColumn)
        {
            foreach (var lexeme in lexemes)
            {
                if (lexeme.Lexeme.TryMatch(source, column, out var matchedText))
                {
                    var endIdx = column + matchedText.Length;
                    if (lexeme.ShouldFailIfAtEndOfLine && endIdx >= source.Length)
                    {
                        throw new LexerException("Unexpected end of line", line, column);
                    }

                    token = new Token(lexeme.Type, matchedText, line, column);
                    newColumn = endIdx;
                    return true;
                }
            }

            token = default;
            newColumn = column;
            return false;
        }

        private class LexemeMetadata(ILexeme lexeme, TokenType type, bool shouldFailIfAtEndOfLine)
        {
            public ILexeme Lexeme { get; init; } = lexeme;
            public TokenType Type { get; init; } = type;
            public bool ShouldFailIfAtEndOfLine { get; init; } = shouldFailIfAtEndOfLine;
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
                        attribute.ShouldFailIfAtEndOfLine));
                }
            }

            return lexemes;
        }

        private readonly List<LexemeMetadata> lexemes;
    }
}
