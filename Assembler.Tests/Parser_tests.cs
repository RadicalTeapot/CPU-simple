using Assembler.Lexeme;

namespace Assembler.Tests
{
    internal static class Helpers
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
            var tokens = Helpers.LexProgram("");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Is.Empty);
        }

        [Test]
        public void SingleLabelOnly_ReturnsProgramNodeWithLabelStatement()
        {
            var tokens = Helpers.LexProgram("START:");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));
            Assert.That(program.Statements[0].Label, Is.Not.Null);

            var label = program.Statements[0].Label;
            Assert.That(label.Label, Is.EqualTo("start"));
        }

        [Test]
        public void SingleDirectiveInstruction_ReturnsProgramNodeWithDirectiveStatement()
        {
            var tokens = Helpers.LexProgram(".DATA");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));
            Assert.That(program.Statements[0].PostDirective, Is.Not.Null);
            var directive = program.Statements[0].PostDirective;
            Assert.That(directive.Directive, Is.EqualTo("data"));
        }

        [Test]
        public void SingleNopInstruction_ReturnsProgramNodeWithNopStatement()
        {
            var tokens = Helpers.LexProgram("NOP");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));
            Assert.That(program.Statements[0].Instruction, Is.Not.Null);

            var instruction = program.Statements[0].Instruction;
            Assert.Multiple(() =>
            {
                Assert.That(instruction.Mnemonic, Is.EqualTo("nop"));
                Assert.That(instruction.OperandNodes, Is.Empty);
            });
        }

        [Test]
        public void LabelAndNopInstruction_ReturnsProgramNodeWithLabelAndInstruction()
        {
            var tokens = Helpers.LexProgram("START: NOP");
            var program = Parser.ParseProgram(tokens);
            Assert.That(program.Statements, Has.Count.EqualTo(1));

            var statement = program.Statements[0];
            Assert.Multiple(() =>
            {
                Assert.That(statement.Label, Is.Not.Null);
                Assert.That(statement.Instruction, Is.Not.Null);
            });

            var label = statement.Label;
            Assert.That(label.Label, Is.EqualTo("start"));

            var instruction = statement.Instruction;
            Assert.Multiple(() =>
            {
                Assert.That(instruction.Mnemonic, Is.EqualTo("nop"));
                Assert.That(instruction.OperandNodes, Is.Empty);
            });
        }

        // TODO
        // Test skip to end of line on error
        // Test directive with operands (single, multiple and string)
        // Test instruction with operands (single and multiple)
    }
}
