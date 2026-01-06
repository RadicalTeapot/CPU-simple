namespace Assembler.Tests
{
    internal static class AnalyserTestsHelper
    {
        public static ProgramNode ParseProgram(string program)
        {
            var tokens = new Lexer().Tokenize(program);
            return Parser.ParseProgram(tokens);
        }
    }

    [TestFixture]
    internal class Analyser_tests
    {
        [Test]
        public void EmptyProgram_NoErrors()
        {
            var programNode = AnalyserTestsHelper.ParseProgram("");
            var emitNodes = new Analyser().Run(programNode);
            Assert.That(emitNodes, Is.Empty);
        }

        [Test]
        public void SimpleNOP_NoErrors()
        {
            var programNode = AnalyserTestsHelper.ParseProgram("NOP");
            var emitNodes = new Analyser().Run(programNode);
            Assert.That(emitNodes, Has.Count.EqualTo(1));
        }
    }
}
