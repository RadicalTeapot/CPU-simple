using Assembler.Lexeme;

namespace Assembler.Tests
{
    [TestFixture]
    internal class Lexer_tests
    {
        [Test]
        public void EmptyInput_ReturnsNoTokens()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void RegisterIsHigherPriorityThanIdentifier()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("r1");
            Assert.That(result, Is.Not.Empty);
            Assert.That(result[0].Type, Is.EqualTo(TokenType.Register));
        }

        [Test]
        public void IdentifierIsHigherPriorityThanHexNumber()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("add");
            Assert.That(result, Is.Not.Empty);
            Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
        }

        [Test]
        public void CommentsAreRemoved()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("nop ; this is a comment");
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
            });
        }

        [Test]
        public void WhitespaceIsTrimmed()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("  nop  ");
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Column, Is.EqualTo(0));
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
            });
        }

        [Test]
        public void EmptyLinesAreIgnored()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("nop\n\nnop");
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Line, Is.EqualTo(0));
                Assert.That(result[1].Line, Is.EqualTo(1));
            });
        }

        [Test]
        public void IsCaseInsensitive()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("NOP");
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
            });
        }

        [Test]
        public void IntegrationTest()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("MOV R1, R2 ; This is a comment\nADDI R1, #01");
            Assert.That(result, Has.Count.EqualTo(9));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
                Assert.That(result[1].Type, Is.EqualTo(TokenType.Register));
                Assert.That(result[2].Type, Is.EqualTo(TokenType.Comma));
                Assert.That(result[3].Type, Is.EqualTo(TokenType.Register));
                Assert.That(result[4].Type, Is.EqualTo(TokenType.Identifier));
                Assert.That(result[5].Type, Is.EqualTo(TokenType.Register));
                Assert.That(result[6].Type, Is.EqualTo(TokenType.Comma));
                Assert.That(result[7].Type, Is.EqualTo(TokenType.Hash));
                Assert.That(result[8].Type, Is.EqualTo(TokenType.HexNumber));
            });
        }
    }
}
