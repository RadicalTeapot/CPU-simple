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
    internal class ImmediateValueNode_tests
    {
        [Test]
        public void ValidImmediate_IsValidHexNodeAtIndex_ReturnsTrue()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 0, 0),
                new Token(TokenType.HexNumber, "0x0", 0, 1)
            };

            var result = ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, 0);
            Assert.That(result, Is.True, "A hash followed by a hex number should be a valid immediate hex");
        }

        [Test]
        public void NonHashStart_IsValidImmediateNodeAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.HexNumber, "0x10", 0, 0) };
            var result = ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Without a leading hash, it is not a valid immediate hex");

            tokens = new[] { new Token(TokenType.Identifier, "label", 0, 0) };
            result = ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Identifier is not a valid immediate hex");
        }

        [Test]
        public void HashWithoutValue_IsValidImmediateNodeAtIndex_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Hash, "#", 0, 0) };
            Assert.Throws<ParserException>(() => ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, 0),
                "A hash without a following immediate value should throw");
        }

        [Test]
        public void HashFollowedByNonHex_IsValidImmediateNodeAtIndex_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 0, 0),
                new Token(TokenType.Identifier, "x", 0, 1)
            };
            Assert.Throws<ParserException>(() => ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, 0),
                "A hash followed by a non-hex token should throw");
        }

        [Test]
        public void ValidImmediate_CreateFromTokens_ReturnsExpectedHexNumberNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 2, 10),
                new Token(TokenType.HexNumber, "0x1A2", 2, 11)
            };

            var result = ImmediateValueNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(ImmediateValueNode.TokenCount, Is.EqualTo(2), "Token count for immediate value node should be 2");
                Assert.That(result.Value, Is.EqualTo("0x1A2"), "Immediate value should be read from the second token");
                Assert.That(result.Span.Line, Is.EqualTo(2), "Span line should be that of the hash token");
                Assert.That(result.Span.StartColumn, Is.EqualTo(10), "Span start should be at the hash token column");
                Assert.That(result.Span.EndColumn, Is.EqualTo(16), "Span end should be hex token column + hex lexeme length");
            });
        }

        [Test]
        public void InvalidImmediate_NoHash_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.HexNumber, "0xFF", 0, 0) };
            Assert.Throws<ParserException>(() => ImmediateValueNode.CreateFromTokens(tokens, 0),
                "CreateFromTokens should throw when the first token is not a hash");
        }

        [Test]
        public void InvalidImmediate_HashWithoutValue_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Hash, "#", 0, 0) };
            Assert.Throws<ParserException>(() => ImmediateValueNode.CreateFromTokens(tokens, 0),
                "CreateFromTokens should throw when a immediate value does not follow the hash");
        }

        [Test]
        public void InvalidImmediate_HashFollowedByNonHex_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Hash, "#", 0, 0),
                new Token(TokenType.String, "\"s\"", 0, 1)
            };
            Assert.Throws<ParserException>(() => ImmediateValueNode.CreateFromTokens(tokens, 0),
                "CreateFromTokens should throw when the token after hash is not a hex number");
        }
    }

    [TestFixture]
    internal class StringLiteralNode_tests
    {
        [Test]
        public void ValidString_IsValidStringOperandNodeAtIndex_ReturnsTrue()
        {
            var tokens = new[] { new Token(TokenType.String, "\"abc\"", 0, 0) };
            var result = StringLiteralNode.IsValidStringOperandNodeAtIndex(tokens, 0);
            Assert.That(result, Is.True, "String token should be a valid string operand");
        }

        [Test]
        public void InvalidString_IsValidStringOperandNodeAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "label", 0, 0) };
            var result = StringLiteralNode.IsValidStringOperandNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Identifier is not a valid string operand");

            tokens = new[] { new Token(TokenType.HexNumber, "0x10", 0, 0) };
            result = StringLiteralNode.IsValidStringOperandNodeAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Hex number is not a valid string operand");
        }

        [Test]
        public void ValidString_CreateFromTokens_ReturnsExpectedStringLiteralNode()
        {
            var tokens = new[] { new Token(TokenType.String, "\"hello\"", 2, 5) };
            var result = StringLiteralNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(StringLiteralNode.TokenCount, Is.EqualTo(1));
                Assert.That(result.Value, Is.EqualTo("\"hello\""));
                Assert.That(result.Span.Line, Is.EqualTo(2));
                Assert.That(result.Span.StartColumn, Is.EqualTo(5));
                Assert.That(result.Span.EndColumn, Is.EqualTo(5 + "\"hello\"".Length));
            });
        }

        [Test]
        public void InvalidString_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "a", 0, 0) };
            Assert.Throws<ParserException>(() => StringLiteralNode.CreateFromTokens(tokens, 0));
        }
    }

    [TestFixture]
    internal class MemoryAddressNode_tests
    {
        [Test]
        public void ValidImmediate_IsValidMemoryAddressAtIndex_ReturnsTrue()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 0, 0),
                new Token(TokenType.Hash, "#", 0, 1),
                new Token(TokenType.HexNumber, "0x1A", 0, 2),
                new Token(TokenType.RightSquareBracket, "]", 0, 6)
            };
            var result = MemoryAddressNode.IsValidMemoryAddressAtIndex(tokens, 0);
            Assert.That(result, Is.True, "[ # hex ] should be a valid memory address");
        }

        [Test]
        public void NonBracketStart_IsValidMemoryAddressAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "x", 0, 0) };
            var result = MemoryAddressNode.IsValidMemoryAddressAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Without leading [, not a valid memory address");
        }

        [Test]
        public void NotEnoughTokens_IsValidMemoryAddressAtIndex_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 0, 0),
                new Token(TokenType.Identifier, "x", 0, 1)
            };
            Assert.Throws<ParserException>(() => MemoryAddressNode.IsValidMemoryAddressAtIndex(tokens, 0));
        }

        [Test]
        public void ValidImmediate_CreateFromTokens_ReturnsExpectedMemoryAddressNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 1, 10),
                new Token(TokenType.Hash, "#", 1, 11),
                new Token(TokenType.HexNumber, "0x1A", 1, 12),
                new Token(TokenType.RightSquareBracket, "]", 1, 16)
            };
            var result = MemoryAddressNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.TokenCount, Is.EqualTo(2 + ImmediateValueNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(1));
                Assert.That(result.Span.StartColumn, Is.EqualTo(10));
                Assert.That(result.Span.EndColumn, Is.EqualTo(17), "End should include the right bracket");
            });

            var addr = result.GetAddress();
            Assert.That(addr, Is.InstanceOf<MemoryAddress.Immediate>());
            var imm = (MemoryAddress.Immediate)addr;
            Assert.That(imm.Address.Value, Is.EqualTo("0x1A"));
        }

        [Test]
        public void ValidLabel_CreateFromTokens_ReturnsExpectedMemoryAddressNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 2, 0),
                new Token(TokenType.Identifier, "label", 2, 1),
                new Token(TokenType.RightSquareBracket, "]", 2, 6)
            };
            var result = MemoryAddressNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.TokenCount, Is.EqualTo(2 + LabelReferenceNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(2));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(7));
            });

            var addr = result.GetAddress();
            Assert.That(addr, Is.InstanceOf<MemoryAddress.Label>());
            var lbl = (MemoryAddress.Label)addr;
            Assert.That(lbl.LabelRef.Label, Is.EqualTo("label"));
        }

        [Test]
        public void ValidLabelWithPositiveOffset_CreateFromTokens_ReturnsExpectedMemoryAddressNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 3, 0),
                new Token(TokenType.Identifier, "buf", 3, 1),
                new Token(TokenType.Plus, "+", 3, 4),
                new Token(TokenType.Hash, "#", 3, 5),
                new Token(TokenType.HexNumber, "0x10", 3, 6),
                new Token(TokenType.RightSquareBracket, "]", 3, 10)
            };
            var result = MemoryAddressNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.TokenCount, Is.EqualTo(2 + LabelReferenceNode.TokenCount + 1 + ImmediateValueNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(3));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(11));
            });

            var addr = result.GetAddress();
            Assert.That(addr, Is.InstanceOf<MemoryAddress.LabelWithPositiveOffset>());
            var lblWithOffset = (MemoryAddress.LabelWithPositiveOffset)addr;
            Assert.Multiple(() =>
            {
                Assert.That(lblWithOffset.LabelRef.Label, Is.EqualTo("buf"));
                Assert.That(lblWithOffset.Offset.Value, Is.EqualTo("0x10"));
            });
        }

        [Test]
        public void ValidLabelWithNegativeOffset_CreateFromTokens_ReturnsExpectedMemoryAddressNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 4, 0),
                new Token(TokenType.Identifier, "buf", 4, 1),
                new Token(TokenType.Minus, "-", 4, 4),
                new Token(TokenType.Hash, "#", 4, 5),
                new Token(TokenType.HexNumber, "0x10", 4, 6),
                new Token(TokenType.RightSquareBracket, "]", 4, 10)
            };
            var result = MemoryAddressNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.TokenCount, Is.EqualTo(2 + LabelReferenceNode.TokenCount + 1 + ImmediateValueNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(4));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(11));
            });

            var addr = result.GetAddress();
            Assert.That(addr, Is.InstanceOf<MemoryAddress.LabelWithNegativeOffset>());
            var lblWithOffset = (MemoryAddress.LabelWithNegativeOffset)addr;
            Assert.Multiple(() =>
            {
                Assert.That(lblWithOffset.LabelRef.Label, Is.EqualTo("buf"));
                Assert.That(lblWithOffset.Offset.Value, Is.EqualTo("0x10"));
            });
        }

        [Test]
        public void InvalidOffsetToken_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 5, 0),
                new Token(TokenType.Identifier, "buf", 5, 1),
                new Token(TokenType.Plus, "+", 5, 4),
                new Token(TokenType.Identifier, "x", 5, 5)
            };
            Assert.Throws<ParserException>(() => MemoryAddressNode.CreateFromTokens(tokens, 0));
        }

        [Test]
        public void MissingClosingBracket_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.LeftSquareBracket, "[", 6, 0),
                new Token(TokenType.Hash, "#", 6, 1),
                new Token(TokenType.HexNumber, "0x10", 6, 2)
            };
            Assert.Throws<ParserException>(() => MemoryAddressNode.CreateFromTokens(tokens, 0));
        }
    }

    [TestFixture]
    internal class DirectiveNode_tests
    {
        [Test]
        public void ValidDirective_NoOperand_CreateFromTokens_ReturnsExpected()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 0, 0),
                new Token(TokenType.Identifier, "text", 0, 1)
            };
            var result = DirectiveNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Directive, Is.EqualTo("text"));
                Assert.That(result.TokenCount, Is.EqualTo(2));
                Assert.That(result.Span.Line, Is.EqualTo(0));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(1 + "text".Length));
            });
        }

        [Test]
        public void NonDotStart_IsValidDirectiveAtIndex_ReturnsFalse()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "a", 0, 0) };
            var result = DirectiveNode.IsValidDirectiveAtIndex(tokens, 0);
            Assert.That(result, Is.False, "Without a leading dot, it is not a valid directive");
        }

        [Test]
        public void DotWithoutValue_IsValidDirectiveAtIndex_ThrowsParserException()
        {
            var tokens = new[] { new Token(TokenType.Dot, ".", 0, 0) };
            Assert.Throws<ParserException>(() => DirectiveNode.IsValidDirectiveAtIndex(tokens, 0),
                "A dot without a following directive should throw");
        }

        [Test]
        public void DotFollowedByNonIdentifier_IsValidDirectiveAtIndex_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 0, 0),
                new Token(TokenType.HexNumber, "0x10", 0, 1)
            };
            Assert.Throws<ParserException>(() => DirectiveNode.IsValidDirectiveAtIndex(tokens, 0),
                "A dot followed by a non-identifier token should throw");
        }

        [Test]
        public void ValidDirective_SingleImmediate_CreateFromTokens_ReturnsExpected()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 1, 0),
                new Token(TokenType.Identifier, "org", 1, 1),
                new Token(TokenType.Hash, "#", 1, 5),
                new Token(TokenType.HexNumber, "0x10", 1, 6)
            };
            var result = DirectiveNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Directive, Is.EqualTo("org"));
                Assert.That(result.TokenCount, Is.EqualTo(2 + ImmediateValueNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(1));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(6 + "0x10".Length));
            });
        }

        [Test]
        public void ValidDirective_TwoImmediates_CreateFromTokens_ReturnsExpected()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 2, 0),
                new Token(TokenType.Identifier, "org", 2, 1),
                new Token(TokenType.Hash, "#", 2, 5),
                new Token(TokenType.HexNumber, "0x10", 2, 6),
                new Token(TokenType.Comma, ",", 2, 10),
                new Token(TokenType.Hash, "#", 2, 11),
                new Token(TokenType.HexNumber, "0xFF", 2, 12)
            };
            var result = DirectiveNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Directive, Is.EqualTo("org"));
                Assert.That(result.TokenCount, Is.EqualTo(2 + ImmediateValueNode.TokenCount + 1 + ImmediateValueNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(2));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(12 + "0xFF".Length));
            });
        }

        [Test]
        public void ValidDirective_SingleString_CreateFromTokens_ReturnsExpected()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 3, 0),
                new Token(TokenType.Identifier, "string", 3, 1),
                new Token(TokenType.String, "\"hello\"", 3, 8)
            };
            var result = DirectiveNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Directive, Is.EqualTo("string"));
                Assert.That(result.TokenCount, Is.EqualTo(2 + StringLiteralNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(3));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(8 + "\"hello\"".Length));
            });
        }

        [Test]
        public void InvalidDirective_CommaWithoutSecondOperand_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 4, 0),
                new Token(TokenType.Identifier, "org", 4, 1),
                new Token(TokenType.Hash, "#", 4, 5),
                new Token(TokenType.HexNumber, "0x10", 4, 6),
                new Token(TokenType.Comma, ",", 4, 10),
                new Token(TokenType.Identifier, "x", 4, 11)
            };
            Assert.Throws<ParserException>(() => DirectiveNode.CreateFromTokens(tokens, 0));
        }
    }

    [TestFixture]
    internal class InstructionNode_tests
    {
        [Test]
        public void ValidMnemonicOnly_CreateFromTokens_ReturnsExpectedInstructionNode()
        {
            var tokens = new[] { new Token(TokenType.Identifier, "nop", 0, 0) };
            var result = InstructionNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Mnemonic, Is.EqualTo("nop"));
                Assert.That(result.TokenCount, Is.EqualTo(1));
                Assert.That(result.Span.Line, Is.EqualTo(0));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(3));
            });

            var operands = result.GetOperands();
            Assert.That(operands, Is.InstanceOf<InstructionOperandSet.None>());
        }

        [Test]
        public void ValidMemoryAddressOnly_CreateFromTokens_ReturnsExpectedInstructionNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Identifier, "jmp", 1, 0),
                new Token(TokenType.LeftSquareBracket, "[", 1, 4),
                new Token(TokenType.Hash, "#", 1, 5),
                new Token(TokenType.HexNumber, "0x10", 1, 6),
                new Token(TokenType.RightSquareBracket, "]", 1, 10),
                new Token(TokenType.EndOfLine, string.Empty, 1, 11)
            };
            var result = InstructionNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Mnemonic, Is.EqualTo("jmp"));
                Assert.That(result.TokenCount, Is.EqualTo(1 + 4));
                Assert.That(result.Span.Line, Is.EqualTo(1));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(11));
            });

            var operands = result.GetOperands();
            Assert.That(operands, Is.InstanceOf<InstructionOperandSet.SingleMemoryAddressOperand>());
            var singleMemOp = (InstructionOperandSet.SingleMemoryAddressOperand)operands;
            var memoryAddr = singleMemOp.Operand.GetAddress();
            Assert.That(memoryAddr, Is.InstanceOf<MemoryAddress.Immediate>());
            var immAddr = (MemoryAddress.Immediate)memoryAddr;
            Assert.That(immAddr.Address.Value, Is.EqualTo("0x10"));
        }

        [Test]
        public void ValidRegisterOnly_CreateFromTokens_ReturnsExpectedInstructionNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Identifier, "ldi", 1, 0),
                new Token(TokenType.Register, "r1", 1, 4),
                new Token(TokenType.EndOfLine, string.Empty, 1, 6)
            };
            var result = InstructionNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Mnemonic, Is.EqualTo("ldi"));
                Assert.That(result.TokenCount, Is.EqualTo(1 + RegisterNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(1));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(6));
            });
            var operands = result.GetOperands();
            Assert.That(operands, Is.InstanceOf<InstructionOperandSet.SingleRegisterOperand>());
            var singleRegOp = (InstructionOperandSet.SingleRegisterOperand)operands;
            Assert.That(singleRegOp.Operand.RegisterName, Is.EqualTo("r1"));
        }

        [Test]
        public void ValidRegisterAndImmediate_CreateFromTokens_ReturnsExpectedInstructionNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Identifier, "adi", 2, 0),
                new Token(TokenType.Register, "r1", 2, 4),
                new Token(TokenType.Comma, ",", 2, 6),
                new Token(TokenType.Hash, "#", 2, 8),
                new Token(TokenType.HexNumber, "0x7f", 2, 9),
                new Token(TokenType.EndOfLine, string.Empty, 2, 13)
            };
            var result = InstructionNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Mnemonic, Is.EqualTo("adi"));
                Assert.That(result.TokenCount, Is.EqualTo(1 + RegisterNode.TokenCount + 1 + ImmediateValueNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(2));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(13));
            });
            var operands = result.GetOperands();
            Assert.That(operands, Is.InstanceOf<InstructionOperandSet.RegisterAndImmediateValueOperand>());
            var regAndHexOp = (InstructionOperandSet.RegisterAndImmediateValueOperand)operands;
            Assert.Multiple(() =>
            {
                Assert.That(regAndHexOp.FirstOperand.RegisterName, Is.EqualTo("r1"));
                Assert.That(regAndHexOp.SecondOperand.Value, Is.EqualTo("0x7f"));
            });
        }

        [Test]
        public void ValidRegisterAndLabel_CreateFromTokens_ReturnsExpectedInstructionNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Identifier, "mov", 3, 0),
                new Token(TokenType.Register, "r2", 3, 4),
                new Token(TokenType.Comma, ",", 3, 6),
                new Token(TokenType.Identifier, "label", 3, 8),
                new Token(TokenType.EndOfLine, string.Empty, 3, 13)
            };
            var result = InstructionNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Mnemonic, Is.EqualTo("mov"));
                Assert.That(result.TokenCount, Is.EqualTo(1 + RegisterNode.TokenCount + 1 + LabelReferenceNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(3));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(13));
            });
            var operands = result.GetOperands();
            Assert.That(operands, Is.InstanceOf<InstructionOperandSet.RegisterAndLabelOperand>());
            var regAndLabelOp = (InstructionOperandSet.RegisterAndLabelOperand)operands;
            Assert.Multiple(() =>
            {
                Assert.That(regAndLabelOp.FirstOperand.RegisterName, Is.EqualTo("r2"));
                Assert.That(regAndLabelOp.SecondOperand.Label, Is.EqualTo("label"));
            });
        }

        [Test]
        public void ValidRegisterAndMemoryAddress_CreateFromTokens_ReturnsExpectedInstructionNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Identifier, "lda", 4, 0),
                new Token(TokenType.Register, "r0", 4, 4),
                new Token(TokenType.Comma, ",", 4, 6),
                new Token(TokenType.LeftSquareBracket, "[", 4, 8),
                new Token(TokenType.Hash, "#", 4, 9),
                new Token(TokenType.HexNumber, "0x10", 4, 10),
                new Token(TokenType.RightSquareBracket, "]", 4, 14),
                new Token(TokenType.EndOfLine, string.Empty, 4, 15)
            };
            var result = InstructionNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Mnemonic, Is.EqualTo("lda"));
                Assert.That(result.TokenCount, Is.EqualTo(1 + RegisterNode.TokenCount + 1 + (2 + ImmediateValueNode.TokenCount)));
                Assert.That(result.Span.Line, Is.EqualTo(4));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(15));
            });
            var operands = result.GetOperands();
            Assert.That(operands, Is.InstanceOf<InstructionOperandSet.RegisterAndMemoryAddressOperand>());
            var regAndMemOp = (InstructionOperandSet.RegisterAndMemoryAddressOperand)operands;
            Assert.That(regAndMemOp.FirstOperand.RegisterName, Is.EqualTo("r0"));
            var memoryAddr = regAndMemOp.SecondOperand.GetAddress();
            Assert.That(memoryAddr, Is.InstanceOf<MemoryAddress.Immediate>());
            var immAddr = (MemoryAddress.Immediate)memoryAddr;
            Assert.That(immAddr.Address.Value, Is.EqualTo("0x10"));
        }

        [Test]
        public void ValidTwoRegisters_CreateFromTokens_ReturnsExpectedInstructionNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Identifier, "add", 5, 0),
                new Token(TokenType.Register, "r1", 5, 4),
                new Token(TokenType.Comma, ",", 5, 6),
                new Token(TokenType.Register, "r2", 5, 8),
                new Token(TokenType.EndOfLine, string.Empty, 5, 10)
            };
            var result = InstructionNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.Mnemonic, Is.EqualTo("add"));
                Assert.That(result.TokenCount, Is.EqualTo(1 + RegisterNode.TokenCount + 1 + RegisterNode.TokenCount));
                Assert.That(result.Span.Line, Is.EqualTo(5));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(10));
            });
            var operands = result.GetOperands();
            Assert.That(operands, Is.InstanceOf<InstructionOperandSet.TwoRegistersOperand>());
            var twoRegOp = (InstructionOperandSet.TwoRegistersOperand)operands;
            Assert.Multiple(() =>
            {
                Assert.That(twoRegOp.FirstOperand.RegisterName, Is.EqualTo("r1"));
                Assert.That(twoRegOp.SecondOperand.RegisterName, Is.EqualTo("r2"));
            });
        }
    }

    [TestFixture]
    internal class StatementNode_tests
    {
        [Test]
        public void HeaderLabelInstruction_CreateFromTokens_ReturnsExpectedStatementNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 0, 0),
                new Token(TokenType.Identifier, "text", 0, 1),
                new Token(TokenType.Identifier, "start", 0, 6),
                new Token(TokenType.Colon, ":", 0, 11),
                new Token(TokenType.Identifier, "nop", 0, 13),
                new Token(TokenType.EndOfLine, string.Empty, 0, 16)
            };
            var result = StatementNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.TokenCount, Is.EqualTo(2 + LabelNode.TokenCount + 1 + 1), "directive + label + instruction + EOL");
                Assert.That(result.Span.Line, Is.EqualTo(0));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(16));
                Assert.That(result.HasHeaderDirective, Is.True);
                Assert.That(result.HasLabel, Is.True);
                Assert.That(result.HasInstruction, Is.True);
                Assert.That(result.HasPostDirective, Is.False);
            });
        }

        [Test]
        public void HeaderLabelPostDirective_CreateFromTokens_ReturnsExpectedStatementNode()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 1, 0),
                new Token(TokenType.Identifier, "text", 1, 1),
                new Token(TokenType.Identifier, "str", 1, 6),
                new Token(TokenType.Colon, ":", 1, 9),
                new Token(TokenType.Dot, ".", 1, 11),
                new Token(TokenType.Identifier, "string", 1, 12),
                new Token(TokenType.String, "\"x\"", 1, 18),
                new Token(TokenType.EndOfLine, string.Empty, 1, 21)
            };
            var result = StatementNode.CreateFromTokens(tokens, 0);
            Assert.Multiple(() =>
            {
                Assert.That(result.TokenCount, Is.EqualTo(2 + LabelNode.TokenCount + (2 + StringLiteralNode.TokenCount) + 1));
                Assert.That(result.Span.Line, Is.EqualTo(1));
                Assert.That(result.Span.StartColumn, Is.EqualTo(0));
                Assert.That(result.Span.EndColumn, Is.EqualTo(21));
                Assert.That(result.HasHeaderDirective, Is.True);
                Assert.That(result.HasLabel, Is.True);
                Assert.That(result.HasPostDirective, Is.True);
                Assert.That(result.HasInstruction, Is.False);
            });
        }

        [Test]
        public void MissingEndOfLine_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Dot, ".", 2, 0),
                new Token(TokenType.Identifier, "text", 2, 1),
                new Token(TokenType.Identifier, "start", 2, 6),
                new Token(TokenType.Colon, ":", 2, 11),
                new Token(TokenType.Identifier, "nop", 2, 13)
            };
            Assert.Throws<ParserException>(() => StatementNode.CreateFromTokens(tokens, 0));
        }

        [Test]
        public void PostAndInstruction_CreateFromTokens_ThrowsParserException()
        {
            var tokens = new[]
            {
                new Token(TokenType.Identifier, "nop", 1, 0),
                new Token(TokenType.Dot, ".", 1, 3),
                new Token(TokenType.Identifier, "string", 1, 4),
                new Token(TokenType.String, "\"x\"", 1, 10),
                new Token(TokenType.EndOfLine, string.Empty, 1, 13)
            };
            Assert.Throws<ParserException>(() => StatementNode.CreateFromTokens(tokens, 0));
        }
    }
}
