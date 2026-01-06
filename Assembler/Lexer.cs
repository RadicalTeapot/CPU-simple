using Assembler.Lexeme;

namespace Assembler
{
    public class LexerException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public LexerException(string message, int line, int column)
            : base($"{message} at line {line}, column {column}")
        {
            Line = line;
            Column = column;
        }
    }

    public class Lexer
    {
        public Lexer() 
        {
            tokenFactory = new TokenFactory();
        }

        public List<Token> Tokenize(string source)
        {
            var tokens = new List<Token>();
            var lastLineNumber = 0;
            foreach (var (line, originalLineNumber) in GetCleanedLinesFromSource(source))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue; // Skip empty lines but preserve original line numbers
                }
                tokens.AddRange(TokenizeLine(line, originalLineNumber));
                lastLineNumber = originalLineNumber;
            }
            tokens.Add(new Token(TokenType.EndOfFile, string.Empty, lastLineNumber + 1, 0));
            return tokens;
        }

        /// <summary>
        /// Tokenizes a single line of source code.
        /// </summary>
        /// <param name="line">Line of source code</param>
        /// <param name="lineNumber">Line number in the source code</param>
        /// <returns>List of tokens extracted from the line</returns>
        /// <exception cref="LexerException"></exception>
        private List<Token> TokenizeLine(string line, int lineNumber)
        {
            var column = 0;
            var tokens = new List<Token>();
            while (column < line.Length)
            {
                // Skip whitespace
                while (column < line.Length && IsWhiteSpace(line[column]))
                {
                    column++;
                }

                if (column >= line.Length)
                {
                    break;
                }

                if (tokenFactory.TryCreateToken(line, lineNumber, column, out var token, out var newColumn))
                {
                    tokens.Add(token);
                    column = newColumn;
                    continue;
                }

                throw new LexerException($"Unrecognized expression '{line[column..]}'", lineNumber, column);
            }

            tokens.Add(new Token(TokenType.EndOfLine, string.Empty, lineNumber, column));
            return tokens;
        }

        /// <summary>
        /// Splits the source code into individual lines and clean them.
        /// </summary>
        /// <param name="source">Original source code</param>
        /// <returns>Enumerable collection of tuples containing cleaned lines and their original line numbers</returns>
        /// <remarks>Comments are trimmed and lines are cast to lowercase. Original line numbers are preserved.</remarks>
        private static IEnumerable<(string Line, int OriginalLineNumber)> GetCleanedLinesFromSource(string source)
        {
            return source
                .Split(NewLineChars, StringSplitOptions.None)
                .Select((line, index) => (Line: line.Split(CommentDelimiter)[0], OriginalLineNumber: index))    // Remove comments
                .Select(item => (Line: item.Line.Trim(), item.OriginalLineNumber))                              // Trim whitespace on both ends
                .Where(item => !string.IsNullOrWhiteSpace(item.Line))                                           // Remove empty lines
                .Select(item => (Line: item.Line.ToLower(), item.OriginalLineNumber));                          // Convert to lowercase
        }

        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t';

        private const string CommentDelimiter = ";";
        private static readonly string[] NewLineChars = ["\r\n", "\n"];
        private readonly TokenFactory tokenFactory;
    }
}
