using Assembler.Lexeme;
using CPU.opcodes;

namespace LanguageServer;

public class TokenLocator
{
    public (Token Token, int Index)? FindTokenAt(List<Token> tokens, int line, int col)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Line == line && col >= token.Column && col < token.Column + token.Length)
            {
                return (token, i);
            }
        }
        return null;
    }

    public bool IsDirectiveContext(List<Token> tokens, int tokenIndex)
    {
        return tokenIndex > 0 && tokens[tokenIndex - 1].Type == TokenType.Dot;
    }

    public bool IsInstructionMnemonic(List<Token> tokens, int tokenIndex)
    {
        var token = tokens[tokenIndex];
        if (token.Type != TokenType.Identifier)
            return false;

        // Check if it's a valid mnemonic
        if (!Enum.TryParse<OpcodeBaseCode>(token.Lexeme, true, out _))
            return false;

        // It's an instruction if it's the first identifier on its line
        // (preceding tokens on the same line should only be label: or section directive stuff)
        for (var i = tokenIndex - 1; i >= 0; i--)
        {
            if (tokens[i].Line != token.Line)
                break;
            // If we find a colon before us on the same line, we're after a label — still the mnemonic
            if (tokens[i].Type == TokenType.Colon)
                return true;
            // If we find another identifier that isn't followed by colon, we're an operand
            if (tokens[i].Type == TokenType.Identifier)
                return false;
            // EndOfLine from previous line
            if (tokens[i].Type == TokenType.EndOfLine)
                break;
        }
        return true;
    }

    public bool IsLabelDefinition(List<Token> tokens, int tokenIndex)
    {
        return tokenIndex + 1 < tokens.Count && tokens[tokenIndex + 1].Type == TokenType.Colon;
    }

    public string? GetMnemonicForLine(List<Token> tokens, int line)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Line == line && token.Type == TokenType.Identifier &&
                Enum.TryParse<OpcodeBaseCode>(token.Lexeme, true, out _) &&
                IsInstructionMnemonic(tokens, i))
            {
                return token.Lexeme;
            }
        }
        return null;
    }
}
