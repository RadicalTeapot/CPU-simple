using Assembler.Analysis;
using Assembler.Analysis.EmitNode;

namespace Assembler.Tests
{
    internal static class AnalyserTestsHelper
    {
        public static Parser.ProgramNode ParseProgram(string program)
        {
            var tokens = new Lexer().Tokenize(program);
            return Parser.ParseProgram(tokens);
        }

        public static IList<IEmitNode> AnalyseProgram(string program)
        {
            var programNode = ParseProgram(program);
            return new Analyser().Run(programNode);
        }

        public static byte[] AnalyseAndEmit(string program)
        {
            var emitNodes = AnalyseProgram(program);
            return new Emitter().Emit(emitNodes);
        }

        public static IList<Symbol> GetSymbols(string program)
        {
            var programNode = ParseProgram(program);
            var analyser = new Analyser();
            analyser.Run(programNode);
            return analyser.GetSymbols();
        }
    }

    [TestFixture]
    internal class Analyser_tests
    {
        // === Empty / Minimal Programs ===

        [Test]
        public void EmptyProgram_NoErrors()
        {
            var emitNodes = AnalyserTestsHelper.AnalyseProgram("");
            Assert.That(emitNodes, Is.Empty);
        }

        [Test]
        public void SimpleNOP_NoErrors()
        {
            var emitNodes = AnalyserTestsHelper.AnalyseProgram("NOP");
            Assert.That(emitNodes, Has.Count.EqualTo(1));
        }

        // === Section Tests ===

        [Test]
        public void DefaultSection_IsText()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("NOP");
            Assert.That(bytes[0], Is.EqualTo(0x00)); // NOP opcode
        }

        [Test]
        public void DataSection_AcceptsDirectives()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(".data\n.byte #0xAB");
            Assert.That(bytes[0], Is.EqualTo(0xAB));
        }

        [Test]
        public void TextSection_SwitchBack()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("NOP\n.data\n.byte #0xFF\n.text\nHLT");
            Assert.Multiple(() =>
            {
                Assert.That(bytes[0], Is.EqualTo(0x00)); // NOP
                Assert.That(bytes[1], Is.EqualTo(0x01)); // HLT
                Assert.That(bytes[2], Is.EqualTo(0xFF)); // .byte from data section placed after text
            });
        }

        [Test]
        public void InstructionInDataSection_Throws()
        {
            var ex = Assert.Throws<AggregateException>(() =>
                AnalyserTestsHelper.AnalyseProgram(".data\nNOP"));
            Assert.That(ex!.InnerExceptions[0], Is.TypeOf<AnalyserException>());
        }

        [Test]
        public void DirectiveInTextSection_Throws()
        {
            var ex = Assert.Throws<AggregateException>(() =>
                AnalyserTestsHelper.AnalyseProgram(".byte #0xAB"));
            Assert.That(ex!.InnerExceptions[0], Is.TypeOf<AnalyserException>());
        }

        // === Label Tests ===

        [Test]
        public void Label_CreatesSymbol()
        {
            var symbols = AnalyserTestsHelper.GetSymbols("start:\nNOP");
            Assert.That(symbols, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(symbols[0].Name, Is.EqualTo("start"));
                Assert.That(symbols[0].Address, Is.EqualTo(0));
            });
        }

        [Test]
        public void ForwardReference_Resolves()
        {
            // jmp uses a memory address operand so it resolves forward references
            var program = "jmp [end]\nNOP\nend:\nHLT";
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(program);
            // jmp is 2 bytes (opcode + address), NOP is 1 byte, so 'end' is at address 3
#if x16
            Assert.That(bytes[1] | (bytes[2] << 8), Is.EqualTo(4)); // 16-bit: jmp(1) + addr(2) + NOP(1) = 4
#else
            Assert.That(bytes[1], Is.EqualTo(3)); // 8-bit: jmp(1) + addr(1) + NOP(1) = 3
#endif
        }

        [Test]
        public void BackwardReference_Resolves()
        {
            var program = "start:\nNOP\njmp [start]";
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(program);
#if x16
            Assert.That(bytes[2] | (bytes[3] << 8), Is.EqualTo(0)); // start is at address 0
#else
            Assert.That(bytes[2], Is.EqualTo(0)); // start is at address 0
#endif
        }

        [Test]
        public void DuplicateLabel_Throws()
        {
            Assert.Throws<ParserException>(() =>
                AnalyserTestsHelper.AnalyseProgram("dup:\nNOP\ndup:\nHLT"));
        }

        [Test]
        public void UndefinedLabel_Throws()
        {
            Assert.Throws<ParserException>(() =>
                AnalyserTestsHelper.AnalyseProgram("jmp [undefined]"));
        }

        [Test]
        public void LabelAcrossSections_ResolvesWithCorrectAddress()
        {
            var program = "NOP\n.data\nval:\n.byte #0x42\n.text\nldi r0, #0x00";
            var symbols = AnalyserTestsHelper.GetSymbols(program);
            var valSymbol = symbols.First(s => s.Name == "val");
            // 'val' is in data section. Text section has NOP(1) + LDI(2) = 3 bytes.
            // Data section starts at offset 3. 'val' is at location 0 within data section.
            Assert.That(valSymbol.Address, Is.EqualTo(3));
        }

        // === Directive Tests ===

        [Test]
        public void ByteDirective_EmitsCorrectValue()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(".data\n.byte #0xAB");
            Assert.That(bytes[0], Is.EqualTo(0xAB));
        }

        [Test]
        public void ShortDirective_EmitsLittleEndian()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(".data\n.short #0x1234");
            Assert.Multiple(() =>
            {
                Assert.That(bytes[0], Is.EqualTo(0x34)); // low byte
                Assert.That(bytes[1], Is.EqualTo(0x12)); // high byte
            });
        }

        [Test]
        public void ZeroDirective_AllocatesNZeros()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(".data\n.zero #0x04");
            Assert.That(bytes.Take(4), Is.All.EqualTo(0x00));
        }

        [Test]
        public void StringDirective_EmitsNullTerminated()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(".data\n.string \"hi\"");
            Assert.Multiple(() =>
            {
                Assert.That(bytes[0], Is.EqualTo((byte)'h'));
                Assert.That(bytes[1], Is.EqualTo((byte)'i'));
                Assert.That(bytes[2], Is.EqualTo(0x00)); // null terminator
            });
        }

        [Test]
        public void OrgDirective_ForwardFills()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(".data\n.byte #0xAA\n.org #0x04\n.byte #0xBB");
            Assert.Multiple(() =>
            {
                Assert.That(bytes[0], Is.EqualTo(0xAA));
                Assert.That(bytes[1], Is.EqualTo(0x00)); // fill
                Assert.That(bytes[2], Is.EqualTo(0x00)); // fill
                Assert.That(bytes[3], Is.EqualTo(0x00)); // fill
                Assert.That(bytes[4], Is.EqualTo(0xBB));
            });
        }

        [Test]
        public void OrgDirective_BackwardAddress_Throws()
        {
            var ex = Assert.Throws<AggregateException>(() =>
                AnalyserTestsHelper.AnalyseProgram(".data\n.byte #0xAA\n.byte #0xBB\n.org #0x01"));
            Assert.That(ex!.InnerExceptions[0], Is.TypeOf<AnalyserException>());
            Assert.That(ex.InnerExceptions[0].Message, Does.Contain(".org"));
        }

        [Test]
        public void OrgDirective_SameAddress_ProducesZeroFill()
        {
            // .byte at offset 0 uses 1 byte, .org #0x01 is exactly at current position
            var bytes = AnalyserTestsHelper.AnalyseAndEmit(".data\n.byte #0xAA\n.org #0x01\n.byte #0xBB");
            Assert.Multiple(() =>
            {
                Assert.That(bytes[0], Is.EqualTo(0xAA));
                Assert.That(bytes[1], Is.EqualTo(0xBB)); // no fill bytes in between
            });
        }

        // === Instruction Tests ===

        [Test]
        public void NoOperandInstruction_EmitsSingleByte()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("HLT");
            Assert.That(bytes[0], Is.EqualTo(0x01)); // HLT opcode
        }

        [Test]
        public void SingleRegisterInstruction_EmitsCorrectly()
        {
            // PSH r1 — opcode encodes register in low bits
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("PSH r1");
            Assert.That(bytes, Has.Length.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void TwoRegisterInstruction_EmitsCorrectly()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("MOV r0, r1");
            Assert.That(bytes, Has.Length.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void RegisterAndImmediateInstruction_EmitsCorrectly()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("LDI r0, #0x42");
            Assert.Multiple(() =>
            {
                Assert.That(bytes, Has.Length.GreaterThanOrEqualTo(2));
                Assert.That(bytes[1], Is.EqualTo(0x42)); // immediate value
            });
        }

        [Test]
        public void SingleMemoryAddressInstruction_EmitsCorrectly()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("jmp [#0x10]");
#if x16
            Assert.That(bytes, Has.Length.GreaterThanOrEqualTo(3));
#else
            Assert.That(bytes, Has.Length.GreaterThanOrEqualTo(2));
#endif
        }

        [Test]
        public void InvalidMnemonic_Throws()
        {
            var ex = Assert.Throws<AggregateException>(() =>
                AnalyserTestsHelper.AnalyseProgram("FAKEINSTR"));
            Assert.That(ex!.InnerExceptions[0], Is.TypeOf<AnalyserException>());
        }

        // === Error Recovery ===

        [Test]
        public void MultipleErrors_CollectedInAggregateException()
        {
            // Two invalid instructions should produce two errors
            var ex = Assert.Throws<AggregateException>(() =>
                AnalyserTestsHelper.AnalyseProgram("FAKE1\nFAKE2"));
            Assert.That(ex!.InnerExceptions, Has.Count.EqualTo(2));
        }

        // === Integration ===

        [Test]
        public void CompleteProgram_TextAndDataWithLabelsAndJumps()
        {
            var program = string.Join("\n", [
                "start:",
                "  ldi r0, #0x01",
                "  jmp [end]",
                ".data",
                "myval:",
                "  .byte #0xFF",
                ".text",
                "end:",
                "  HLT"
            ]);

            var bytes = AnalyserTestsHelper.AnalyseAndEmit(program);
            Assert.That(bytes, Has.Length.GreaterThan(0));

            var symbols = AnalyserTestsHelper.GetSymbols(program);
            Assert.That(symbols, Has.Count.EqualTo(3)); // start, end, myval
        }
    }
}
