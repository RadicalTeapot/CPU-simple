using Assembler.Lexeme;

namespace Assembler.Tests
{
    [TestFixture]
    internal class HashLexeme_tests
    {
        [Test]
        public void HashLexeme_CorrectlyIdentifiesHashSymbol()
        {
            var lexeme = new Lexer();
            var result = lexeme.Tokenize("#02");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Hash));
                Assert.That(result[0].Lexeme, Is.EqualTo("#"));
                Assert.That(result[0].Line, Is.EqualTo(0));
                Assert.That(result[0].Column, Is.EqualTo(0));
            });
        }

        [Test]
        public void HashAtEndOfLine_ThrowsLexerException()
        {
            var lexeme = new Lexer();
            var ex = Assert.Throws<LexerException>(() => lexeme.Tokenize("#"));
            Assert.Multiple(() =>
            {
                Assert.That(ex.Line, Is.EqualTo(0));
                Assert.That(ex.Column, Is.EqualTo(0));
            });
        }
    }

    [TestFixture]
    internal class HexNumberLexeme_tests
    {
        [Test]
        public void HexNumberLexeme_CorrectlyIdentifiesHexNumber()
        {
            var lexeme = new Lexer();
            var result = lexeme.Tokenize("0A3F");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.HexNumber));
                Assert.That(result[0].Lexeme, Is.EqualTo("0A3F"));
            });
        }
    }
}
