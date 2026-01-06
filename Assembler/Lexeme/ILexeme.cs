namespace Assembler.Lexeme
{
    internal interface ILexeme
    {
        /// <summary>
        /// Tries to match the lexeme at the given column in the source string.
        /// </summary>
        /// <param name="source">Source string to match against.</param>
        /// <param name="column">Column index to start matching from (assumed to be within the bounds of the source string).</param>
        /// <param name="matchedText">Output parameter for the matched text.</param>
        /// <returns>True if the lexeme matches; otherwise, false.</returns>
        bool TryMatch(string source, int column, out string matchedText);
    }
}
