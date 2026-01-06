using Assembler.Lexeme;

namespace Assembler.Tests
{
    [TestFixture]
    internal class Lexer_tests
    {
        [Test]
        public void EmptyInput_ReturnsEndOfFileToken()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("");
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Type, Is.EqualTo(TokenType.EndOfFile));
        }

        [Test]
        public void SingleLine_HasEndOfLineAndEndOfFileToken()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("nop");
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[1].Type, Is.EqualTo(TokenType.EndOfLine));
            Assert.That(result[2].Type, Is.EqualTo(TokenType.EndOfFile));
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
        public void CommentsAreRemoved()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("nop ; this is a comment");
            Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
        }

        [Test]
        public void WhitespaceIsTrimmed()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("  nop  ");
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Column, Is.EqualTo(0));
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
            });
        }

        [Test]
        public void WhitespaceBetweenTokensIsIgnored()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("# 0x01");
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Hash));
                Assert.That(result[1].Type, Is.EqualTo(TokenType.HexNumber));
            });
        }

        [Test]
        public void EmptyLinesPreserveOriginalLineNumbers()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("nop\n\nnop");
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Line, Is.EqualTo(0)); // first nop on line 0
                Assert.That(result[2].Line, Is.EqualTo(2)); // second nop on line 2 (line 1 is empty)
            });
        }

        [Test]
        public void IsCaseInsensitive()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("NOP");
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
            });
        }

        [Test]
        public void IntegrationTest()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("MOV R1, R2 ; This is a comment\nADDI R1, #0x01");
            Assert.That(result, Has.Count.EqualTo(12));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));
                Assert.That(result[1].Type, Is.EqualTo(TokenType.Register));
                Assert.That(result[2].Type, Is.EqualTo(TokenType.Comma));
                Assert.That(result[3].Type, Is.EqualTo(TokenType.Register));
                Assert.That(result[4].Type, Is.EqualTo(TokenType.EndOfLine));
                Assert.That(result[5].Type, Is.EqualTo(TokenType.Identifier));
                Assert.That(result[6].Type, Is.EqualTo(TokenType.Register));
                Assert.That(result[7].Type, Is.EqualTo(TokenType.Comma));
                Assert.That(result[8].Type, Is.EqualTo(TokenType.Hash));
                Assert.That(result[9].Type, Is.EqualTo(TokenType.HexNumber));
                Assert.That(result[10].Type, Is.EqualTo(TokenType.EndOfLine));
                Assert.That(result[11].Type, Is.EqualTo(TokenType.EndOfFile));
            });
        }
    }
}
