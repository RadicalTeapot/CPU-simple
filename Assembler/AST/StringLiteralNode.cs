using Assembler.Lexeme;

namespace Assembler.AST
{
    public class StringLiteralNode(string value, NodeSpan span) : BaseNode(span)
    {
        public string Value { get; } = value;

        public const int TokenCount = 1;
        
        public static bool IsValidStringOperandNodeAtIndex(IList<Lexeme.Token> tokens, int index)
        {
            return tokens[index].Type == TokenType.String;
        }
        
        public static StringLiteralNode CreateFromTokens(IList<Lexeme.Token> tokens, int index)
        {
            if (!IsValidStringOperandNodeAtIndex(tokens, index))
            {
                throw new ParserException("Invalid string operand syntax.", tokens[index].Line, tokens[index].Column);
            }
            var stringToken = tokens[index];
            var value = stringToken.Lexeme;
            var span = new NodeSpan(stringToken.Column, stringToken.Column + stringToken.Lexeme.Length, stringToken.Line);
            return new StringLiteralNode(value, span);
        }
    }
}
