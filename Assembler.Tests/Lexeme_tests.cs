using Assembler.Lexeme;

namespace Assembler.Tests
{
    [TestFixture]
    internal class HashLexeme_tests
    {
        [Test]
        public void HashLexeme_CorrectlyIdentifiesHashSymbol()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("#0x02");
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
            var lexer = new Lexer();
            var ex = Assert.Throws<LexerException>(() => lexer.Tokenize("#"));
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
            var lexer = new Lexer();
            var result = lexer.Tokenize("0x0a3f");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.HexNumber));
                Assert.That(result[0].Lexeme, Is.EqualTo("0x0a3f"));
            });
        }

        [Test]
        public void HexNumberLexeme_RequiresPrefix()
        {
            var lexer = new Lexer();

            var result = lexer.Tokenize("a3");
            Assert.That(result, Is.Not.Empty);
            // If number is also a valid identifier (starts with a letter) it should be parsed as an identifier
            Assert.That(result[0].Type, Is.EqualTo(TokenType.Identifier));

            // If number is not a valid identifier, it should throw
            Assert.Throws<LexerException>(() => lexer.Tokenize("03"));
        }

        [Test]
        public void HexNumberLexeme_RequiresAtLeastOneDigitAfterPrefix()
        {
            var lexer = new Lexer();
            Assert.Throws<LexerException>(() => lexer.Tokenize("0x"));
        }
    }

    [TestFixture]
    internal class ColonLexeme_tests
    {
        [Test]
        public void ColonLexeme_CorrectlyIdentifiesColon()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize(":");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Colon));
                Assert.That(result[0].Lexeme, Is.EqualTo(":"));
            });
        }
    }

    [TestFixture]
    internal class CommaLexeme_tests
    {
        [Test]
        public void CommaLexeme_CorrectlyIdentifiesComma()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize(", #0x01");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Comma));
                Assert.That(result[0].Lexeme, Is.EqualTo(","));
            });
        }

        [Test]
        public void CommaAtEndOfLine_ThrowsLexerException()
        {
            var lexer = new Lexer();
            var ex = Assert.Throws<LexerException>(() => lexer.Tokenize(","));
            Assert.Multiple(() =>
            {
                Assert.That(ex.Line, Is.EqualTo(0));
                Assert.That(ex.Column, Is.EqualTo(0));
            });
        }
    }

    [TestFixture]
    internal class DotLexeme_tests
    {
        [Test]
        public void DotLexeme_CorrectlyIdentifiesDot()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize(".a");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Dot));
                Assert.That(result[0].Lexeme, Is.EqualTo("."));
            });
        }

        [Test]
        public void DotAtEndOfLine_ThrowsLexerException()
        {
            var lexer = new Lexer();
            var ex = Assert.Throws<LexerException>(() => lexer.Tokenize("."));
            Assert.Multiple(() =>
            {
                Assert.That(ex.Line, Is.EqualTo(0));
                Assert.That(ex.Column, Is.EqualTo(0));
            });
        }
    }

    [TestFixture]
    internal class OffsetLexeme_tests
    {
        [Test]
        public void PositiveOffsetLexeme_CorrectlyIdentifiesPlus()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("+ #0x01");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Plus));
                Assert.That(result[0].Lexeme, Is.EqualTo("+"));
            });
        }

        [Test]
        public void PositiveOffsetAtEndOfLine_ThrowsLexerException()
        {
            var lexer = new Lexer();
            var ex = Assert.Throws<LexerException>(() => lexer.Tokenize("+"));
            Assert.Multiple(() =>
            {
                Assert.That(ex.Line, Is.EqualTo(0));
                Assert.That(ex.Column, Is.EqualTo(0));
            });
        }

        [Test]
        public void NegativeOffsetLexeme_CorrectlyIdentifiesMinus()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("- #0x01");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Minus));
                Assert.That(result[0].Lexeme, Is.EqualTo("-"));
            });
        }

        [Test]
        public void NegativeOffsetAtEndOfLine_ThrowsLexerException()
        {
            var lexer = new Lexer();
            var ex = Assert.Throws<LexerException>(() => lexer.Tokenize("-"));
            Assert.Multiple(() =>
            {
                Assert.That(ex.Line, Is.EqualTo(0));
                Assert.That(ex.Column, Is.EqualTo(0));
            });
        }
    }

    [TestFixture]
    internal class RegisterLexeme_tests
    {
        [Test]
        public void RegisterLexeme_CorrectlyIdentifiesRegister()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("r1");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.Register));
                Assert.That(result[0].Lexeme, Is.EqualTo("r1"));
            });
        }
    }

    [TestFixture]
    internal class SquareBracketLexeme_tests
    {
        [Test]
        public void LeftSquareBracketLexeme_CorrectlyIdentifiesLeftSquareBracket()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("[a");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.LeftSquareBracket));
                Assert.That(result[0].Lexeme, Is.EqualTo("["));
            });
        }

        [Test]
        public void LeftSquareBracketAtEndOfLine_ThrowsLexerException()
        {
            var lexer = new Lexer();
            var ex = Assert.Throws<LexerException>(() => lexer.Tokenize("["));
            Assert.Multiple(() =>
            {
                Assert.That(ex.Line, Is.EqualTo(0));
                Assert.That(ex.Column, Is.EqualTo(0));
            });
        }

        [Test]
        public void RightSquareBracketLexeme_CorrectlyIdentifiesRightSquareBracket()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("]");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.RightSquareBracket));
                Assert.That(result[0].Lexeme, Is.EqualTo("]"));
            });
        }
    }

    [TestFixture]
    internal class StringLexeme_tests
    {
        [Test]
        public void StringLexeme_CorrectlyIdentifiesString()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("\"hello\"");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(result[0].Lexeme, Is.EqualTo("\"hello\""));
            });
        }

        [Test]
        public void StringLexeme_HandlesEscapedQuote()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("\"hello\\\"world\"");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(result[0].Lexeme, Is.EqualTo("\"hello\\\"world\""));
            });
        }

        [Test]
        public void StringLexeme_HandlesEscapedBackslash()
        {
            var lexer = new Lexer();
            var result = lexer.Tokenize("\"path\\\\file\"");
            Assert.That(result, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Type, Is.EqualTo(TokenType.String));
                Assert.That(result[0].Lexeme, Is.EqualTo("\"path\\\\file\""));
            });
        }
    }
}
