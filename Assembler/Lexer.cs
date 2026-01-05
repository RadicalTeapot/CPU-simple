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
            var column = 0;

            foreach (var line in GetCleanedLinesFromSource(source))
            {
                column = 0;
                while (column < line.Length)
                {
                    ConsumeWhiteSpaces(line, ref column);
                    if (column >= line.Length)
                    {
                        break;
                    }

                    if (tokenFactory.TryCreateToken(line, row, column, out var token, out var newColumn))
                    {
                        tokens.Add(token);
                        column = newColumn;
                        continue;
                    }

                    // Additional token parsing logic would go here...
                    throw new LexerException($"Unrecognized character '{line[column]}'", row, column);
                }
                row++;
            }

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

        private static void ConsumeWhiteSpaces(string source, ref int column)
        {
            while (column < source.Length && IsWhiteSpace(source[column]))
            {
                column++;
            }
        }

        //private static bool IsIdentifierStart(char c)
        //{
        //    return char.IsLetter(c) || c == '_';
        //}

        //private static bool IsRegister(string lexeme)
        //{
        //    return lexeme.StartsWith("r") && lexeme.Length > 1
        //        && lexeme.Skip(1).All(char.IsDigit);
        //}

        //private static bool IsLabel(string lexeme)
        //{
        //    return IsIdentifier(lexeme) && lexeme.EndsWith(":");
        //}

        //private static bool IsDirective(string lexeme)
        //{
        //    return lexeme.StartsWith(".") && lexeme.Length > 1 && IsIdentifier(lexeme[1..]);
        //}

        private const string CommentDelimiter = ";";
        private static readonly string[] NewLineChars = ["\r\n", "\n"];
        private readonly TokenFactory tokenFactory;
    }
}
