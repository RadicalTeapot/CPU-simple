using Assembler.Lexeme;

namespace Assembler.Tests
{
    internal static class ParserTestsHelper
    {
        public static IList<Token> LexProgram(string program)
        {
            var lexer = new Lexer();
            return lexer.Tokenize(program);
        }
    }

    [TestFixture]
    internal class Parser_tests
    {
        [Test]
        public void EmptyProgram_ReturnsEmptyProgramNode()
        {
            var tokens = ParserTestsHelper.LexProgram("");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Is.Empty);
        }

        [Test]
        public void SingleLabelOnly_ReturnsProgramNodeWithLabelStatement()
        {
            var tokens = ParserTestsHelper.LexProgram("START:");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));
            Assert.That(program.Statements[0].GetLabel(), Is.Not.Null);

            var label = program.Statements[0].GetLabel();
            Assert.That(label.Label, Is.EqualTo("start"));
        }

        [Test]
        public void HeaderDirectiveInstruction_ReturnsProgramNodeWithHeaderDirectiveStatement()
        {
            var tokens = ParserTestsHelper.LexProgram(".DATA");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));
            Assert.That(program.Statements[0].GetHeaderDirective(), Is.Not.Null);
            var directive = program.Statements[0].GetHeaderDirective();
            Assert.That(directive.Directive, Is.EqualTo("data"));
        }

        [Test]
        public void SingleNopInstruction_ReturnsProgramNodeWithNopStatement()
        {
            var tokens = ParserTestsHelper.LexProgram("NOP");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));
            Assert.That(program.Statements[0].GetInstruction(), Is.Not.Null);

            var instruction = program.Statements[0].GetInstruction();
            Assert.Multiple(() =>
            {
                Assert.That(instruction.Mnemonic, Is.EqualTo("nop"));
                Assert.That(instruction.HasSignature([]), Is.True);
            });
        }

        [Test]
        public void LabelAndNopInstruction_ReturnsProgramNodeWithLabelAndInstruction()
        {
            var tokens = ParserTestsHelper.LexProgram("START: NOP");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));

            var statement = program.Statements[0];
            Assert.Multiple(() =>
            {
                Assert.That(statement.GetLabel(), Is.Not.Null);
                Assert.That(statement.GetInstruction(), Is.Not.Null);
            });

            var label = statement.GetLabel();
            Assert.That(label.Label, Is.EqualTo("start"));

            var instruction = statement.GetInstruction();
            Assert.Multiple(() =>
            {
                Assert.That(instruction.Mnemonic, Is.EqualTo("nop"));
                Assert.That(instruction.HasSignature([]), Is.True);
            });
        }

        // TODO
        // Test label followed by directive
        // Test skip to end of line on error
        // Test directive with operands (single, multiple and string)
        // Test instruction with operands (single and multiple)
    }
}
