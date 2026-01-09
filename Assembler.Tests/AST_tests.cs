using Assembler.AST;
using Assembler.Lexeme;

namespace Assembler.Tests
{
    [TestFixture]
    internal class LabelNode_tests
    {
        [Test]
        public void ValidLabel_IsValidLabelAtIndex_ReturnsTrue()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "a", 0, 0), new Token(TokenType.Colon, ":", 0, 1) };
            var result = LabelNode.IsValidLabelAtIndex(tokens, 0);
            Assert.That(result, Is.True, "An identifier followed by a colon should be a valid label");
        }

        [Test]
        public void InvalidLabel_IsValidLabelAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "a", 0, 0) };
            var result = LabelNode.IsValidLabelAtIndex(tokens, 0);
            Assert.That(result, Is.False, "An identifier alone should not be a valid label (missing terminating colon)");

            tokens = [new Token(TokenType.Colon, ":", 0, 0)];
            result = LabelNode.IsValidLabelAtIndex(tokens, 0);
            Assert.That(result, Is.False, "A label cannot start with a colon");

            tokens = [new Token(TokenType.Identifier, "a", 0, 0), new Token(TokenType.Dot, ".", 0, 1)];
            result = LabelNode.IsValidLabelAtIndex(tokens, 0);
            Assert.That(result, Is.False, "A label must end with a colon");
        }

        [Test]
        public void ValidLabel_CreateFromTokens_ReturnsExpectedLabelNode()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "label", 1, 2), new Token(TokenType.Colon, ":", 1, 7) };
            var result = LabelNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(LabelNode.TokenCount, Is.EqualTo(2), "Token count for label node should be 2");
                Assert.That(result.Label, Is.EqualTo("label"), "Label name should be read properly");
                Assert.That(result.Span.Line, Is.EqualTo(1), "Span line should be correct");
                Assert.That(result.Span.StartColumn, Is.EqualTo(2), "Span start column should be start of label");
                Assert.That(result.Span.EndColumn, Is.EqualTo(8), "Span start column should be end of colon token + colon lexeme length");
            });
        }

        [Test]
        public void InvalidReference_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Colon, ":", 0, 0) };
            Assert.Throws<ParserException>(() => LabelNode.CreateFromTokens(tokens, 0), "Invalid label should throw ParserException");
        }
    }

    [TestFixture]
    internal class LabelReferenceNode_tests
    {
        [Test]
        public void ValidReference_IsValidLabelReferenceAtIndex_ReturnsTrue()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "label", 0, 0) };
            var result = LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, 0);
            Assert.That(result, Is.True, "Identifier should be a valid label reference");
        }

        [Test]
        public void InvalidReference_IsValidLabelReferenceAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.Colon, ":", 0, 0) };
            var result = LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Colon is not a valid label reference");

            tokens = [new Token(TokenType.HexNumber, "0x12", 0, 0)];
            result = LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Hex number is not a valid label reference");

            tokens = [new Token(TokenType.String, "\"str\"", 0, 0)];
            result = LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, 0);
            Assert.That(result, Is.False, "String is not a valid label reference");
        }

        [Test]
        public void ValidReference_CreateFromTokens_ReturnsExpectedLabelReferenceNode()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "label", 1, 2) };
            var result = LabelReferenceNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(LabelReferenceNode.TokenCount, Is.EqualTo(1), "Token count for label reference node should be 1");
                Assert.That(result.Label, Is.EqualTo("label"), "Label reference name should be read properly");
                Assert.That(result.Span.Line, Is.EqualTo(1), "Span line should be correct");
                Assert.That(result.Span.StartColumn, Is.EqualTo(2), "Span start column should match start of identifier");
                Assert.That(result.Span.EndColumn, Is.EqualTo(7), "Span end column should be start + identifier lexeme length");
            });
        }

        [Test]
        public void InvalidReference_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Colon, ":", 0, 0) };
            Assert.Throws<ParserException>(() => LabelReferenceNode.CreateFromTokens(tokens, 0), "Invalid label reference should throw ParserException");
        }
    }

    [TestFixture]
    internal class RegisterNode_tests
    {
        [Test]
        public void ValidRegister_IsValidRegisterNodeAtIndex_ReturnsTrue()
        {
            var tokens = new[] { new Token(TokenType.Register, "r1", 0, 0) };
            var result = RegisterNode.IsValidRegisterNodeAtIndex(tokens, 0);
            Assert.That(result, Is.True, "Register token should be a valid register operand");
        }

        [Test]
        public void InvalidRegister_IsValidRegisterNodeAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "label", 0, 0) };
            var result = RegisterNode.IsValidRegisterNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Identifier is not a valid register operand");

            tokens = [new Token(TokenType.HexNumber, "0x10", 0, 0)];
            result = RegisterNode.IsValidRegisterNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Hex number is not a valid register operand");

            tokens = [new Token(TokenType.String, "\"s\"", 0, 0)];
            result = RegisterNode.IsValidRegisterNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "String is not a valid register operand");

            tokens = [new Token(TokenType.Colon, ":", 0, 0)];
            result = RegisterNode.IsValidRegisterNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Colon is not a valid register operand");
        }

        [Test]
        public void ValidRegister_CreateFromTokens_ReturnsExpectedRegisterNode()
        {
            var tokens = new[] { new Token(TokenType.Register, "R12", 3, 7) };
            var result = RegisterNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(RegisterNode.TokenCount, Is.EqualTo(1), "Token count for register node should be 1");
                Assert.That(result.RegisterName, Is.EqualTo("R12"), "Register name should be read properly");
                Assert.That(result.Span.Line, Is.EqualTo(3), "Span line should match token line");
                Assert.That(result.Span.StartColumn, Is.EqualTo(7), "Span start column should match token column");
                Assert.That(result.Span.EndColumn, Is.EqualTo(10), "Span end column should be start + register lexeme length");
            });
        }

        [Test]
        public void InvalidRegister_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "a", 0, 0) };
            Assert.Throws<ParserException>(() => RegisterNode.CreateFromTokens(tokens, 0), "Invalid register operand should throw ParserException");
        }
    }

    [TestFixture]
    internal class HexNumberNode_tests
    {
        [Test]
        public void ValidHex_IsValidHexNodeAtIndex_ReturnsTrue()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 0, 0),
                new Token(TokenType.HexNumber, "0x0", 0, 1)
            };

            var result = HexNumberNode.IsValidHexNodeAtIndex(tokens, 0);
            Assert.That(result, Is.True, "A hash followed by a hex number should be a valid immediate hex");
        }

        [Test]
        public void NonHashStart_IsValidHexNodeAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.HexNumber, "0x10", 0, 0) };
            var result = HexNumberNode.IsValidHexNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Without a leading hash, it is not a valid immediate hex");

            tokens = new[] { new Token(TokenType.Identifier, "label", 0, 0) };
            result = HexNumberNode.IsValidHexNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Identifier is not a valid immediate hex");
        }

        [Test]
        public void HashWithoutValue_IsValidHexNodeAtIndex_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Hash, "#", 0, 0) };
            Assert.Throws<ParserException>(() => HexNumberNode.IsValidHexNodeAtIndex(tokens, 0),
                "A hash without a following hex number should throw");
        }

        [Test]
        public void HashFollowedByNonHex_IsValidHexNodeAtIndex_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 0, 0),
                new Token(TokenType.Identifier, "x", 0, 1)
            };
            Assert.Throws<ParserException>(() => HexNumberNode.IsValidHexNodeAtIndex(tokens, 0),
                "A hash followed by a non-hex token should throw");
        }

        [Test]
        public void ValidHex_CreateFromTokens_ReturnsExpectedHexNumberNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 2, 10),
                new Token(TokenType.HexNumber, "0x1A2", 2, 11)
            };

            var result = HexNumberNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(HexNumberNode.TokenCount, Is.EqualTo(2), "Token count for hex number node should be 2");
                Assert.That(result.Value, Is.EqualTo("0x1A2"), "Hex value should be read from the second token");
                Assert.That(result.Span.Line, Is.EqualTo(2), "Span line should be that of the hash token");
                Assert.That(result.Span.StartColumn, Is.EqualTo(10), "Span start should be at the hash token column");
                Assert.That(result.Span.EndColumn, Is.EqualTo(16), "Span end should be hex token column + hex lexeme length");
            });
        }

        [Test]
        public void InvalidHex_NoHash_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.HexNumber, "0xFF", 0, 0) };
            Assert.Throws<ParserException>(() => HexNumberNode.CreateFromTokens(tokens, 0),
                "CreateFromTokens should throw when the first token is not a hash");
        }

        [Test]
        public void InvalidHex_HashWithoutValue_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Hash, "#", 0, 0) };
            Assert.Throws<ParserException>(() => HexNumberNode.CreateFromTokens(tokens, 0),
                "CreateFromTokens should throw when a hex number does not follow the hash");
        }

        [Test]
        public void InvalidHex_HashFollowedByNonHex_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 0, 0),
                new Token(TokenType.String, "\"s\"", 0, 1)
            };
            Assert.Throws<ParserException>(() => HexNumberNode.CreateFromTokens(tokens, 0),
                "CreateFromTokens should throw when the token after hash is not a hex number");
        }
    }

    // TODO
    // StringLiteralNode tests
    // DirectiveNode tests
    // InstructionNode tests
    // StatementNode tests
    // MemoryAddressNode tests
}
