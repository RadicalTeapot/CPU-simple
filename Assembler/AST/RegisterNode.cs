using Assembler.Lexeme;

namespace Assembler.AST
{
    public class RegisterNode(string registerName, NodeSpan span) : BaseNode(span)
    {
        public string RegisterName { get; } = registerName;

        public const int TokenCount = 1;
        
        public static bool IsValidRegisterNodeAtIndex(IList<Lexeme.Token> tokens, int index)
        {
            return tokens[index].Type == TokenType.Register;
        }

        public static RegisterNode CreateFromTokens(IList<Lexeme.Token> tokens, int index)
        {
            if (!IsValidRegisterNodeAtIndex(tokens, index))
            {
                throw new ParserException("Invalid register operand syntax.", tokens[index].Line, tokens[index].Column);
            }
            var registerToken = tokens[index];
            var value = registerToken.Lexeme;
            var span = new NodeSpan(registerToken.Column, registerToken.Column + registerToken.Lexeme.Length, registerToken.Line);
            return new RegisterNode(value, span);
        }
    }
}
