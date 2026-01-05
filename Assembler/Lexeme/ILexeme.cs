namespace Assembler.Lexeme
{
    internal interface ILexeme
    {
        bool TryMatch(string source, int column, out string matchedText);
    }
}
