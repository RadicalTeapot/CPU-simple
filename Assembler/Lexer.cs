using Assembler.Lexeme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

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
            var row = 0;
            foreach (var line in GetCleanedLinesFromSource(source))
            {
                tokens.AddRange(TokenizeLine(line, row));
                row++;
            }
            tokens.Add(new Token(TokenType.EndOfFile, string.Empty, row, 0));
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
        /// <returns>Enumerable collection of cleaned lines</returns>
        /// <remarks>Empty lines are removed, comments are trimmed and lines are cast to lowercase.</remarks>
        private static IEnumerable<string> GetCleanedLinesFromSource(string source)
        {
            return source
                .Split(NewLineChars, StringSplitOptions.None)
                .Select(line => line.Split(CommentDelimiter)[0])    // Remove comments (everything after ';')
                .Select(line => line.Trim())                        // Trim whitespace on both ends
                .Select(line => line.ToLower())                     // Convert to lowercase
                .Where(line => !string.IsNullOrWhiteSpace(line));   // Remove empty lines
        }

        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t';

        private const string CommentDelimiter = ";";
        private static readonly string[] NewLineChars = ["\r\n", "\n"];
        private readonly TokenFactory tokenFactory;
    }
}
