using Assembler.AST;
using Assembler.Lexeme;

namespace Assembler
{
    public class ParserException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public ParserException(string message, int line, int column)
            : base($"{message} at line {line}, column {column}")
        {
            Line = line;
            Column = column;
        }
    }

    public class Parser
    {
        public class Program(IList<StatementNode> statements)
        {
            public IList<StatementNode> Statements { get; } = statements;
        }

        public static Program ParseProgram(IList<Token> tokens)
        {
            var currentTokenIndex = 0;
            var statements = new List<StatementNode>();
            var parsingErrors = new List<ParserException>();
            while (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type != TokenType.EndOfFile)
            {
                try
                {
                    // Skip any EndOfLine tokens between statements
                    while (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type == TokenType.EndOfLine)
                    {
                        currentTokenIndex++;
                    }

                    if (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type != TokenType.EndOfFile)
                    {
                        var statement = StatementNode.CreateFromTokens(tokens, currentTokenIndex);
                        currentTokenIndex += statement.TokenCount;
                        statements.Add(statement);
                    }
                }
                catch (ParserException ex)
                {
                    parsingErrors.Add(ex);
                    // Attempt to recover by skipping to the next EndOfLine or EndOfFile
                    while (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type != TokenType.EndOfLine && tokens[currentTokenIndex].Type != TokenType.EndOfFile)
                    {
                        currentTokenIndex++;
                    }
                }
            }

            if (tokens[currentTokenIndex].Type != TokenType.EndOfFile)
            {
                parsingErrors.Add(new ParserException("Expected end of file token.", tokens[currentTokenIndex].Line, tokens[currentTokenIndex].Column));
            }

            if (parsingErrors.Count > 0)
            {
                throw new AggregateException("Parsing failed with errors.", parsingErrors);
            }

            return new Program(statements);
        }
    }
}