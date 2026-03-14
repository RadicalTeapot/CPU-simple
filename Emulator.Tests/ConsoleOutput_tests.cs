using Emulator.IO;

namespace Emulator.Tests
{
    [TestFixture]
    internal class ConsoleOutput_tests
    {
        [Test]
        public void StatusSuppressed_DefaultIsFalse()
        {
            var output = new ConsoleOutput();
            Assert.That(output.StatusSuppressed, Is.False);
        }

        [Test]
        public void WriteStatus_WhenNotSuppressed_WritesToConsole()
        {
            var output = new ConsoleOutput();
            var inspector = new CPU.CPU(new CPU.Config()).GetInspector();

            var captured = CaptureConsoleOut(() => output.WriteStatus(inspector));

            Assert.That(captured, Is.Not.Empty);
        }

        [Test]
        public void WriteStatus_WhenSuppressed_WritesNothing()
        {
            var output = new ConsoleOutput();
            output.StatusSuppressed = true;
            var inspector = new CPU.CPU(new CPU.Config()).GetInspector();

            var captured = CaptureConsoleOut(() => output.WriteStatus(inspector));

            Assert.That(captured, Is.Empty);
        }

        [Test]
        public void WriteStatus_SuppressionDoesNotAffectOtherMethods()
        {
            var output = new ConsoleOutput();
            output.StatusSuppressed = true;

            var captured = CaptureConsoleOut(() => output.WriteBreakpointHit(42));

            Assert.That(captured, Is.Not.Empty);
        }

        private static string CaptureConsoleOut(Action action)
        {
            var sb = new System.Text.StringBuilder();
            using var writer = new System.IO.StringWriter(sb);
            var previous = Console.Out;
            Console.SetOut(writer);
            try
            {
                action();
            }
            finally
            {
                Console.SetOut(previous);
            }
            return sb.ToString();
        }
    }
}
