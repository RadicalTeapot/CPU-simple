using Backend.Commands;
using Backend.Commands.GlobalCommands;

namespace Backend.Tests
{
    [TestFixture]
    internal class Dump_tests
    {
        private Dump _command = null!;
        private GlobalCommandExecutionContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _command = new Dump(new CommandContext("dump", "Dump full cpu state", "Usage: 'dump [status] [memory] [stack]'"));
            _context = BackendTestHelpers.CreateGlobalContext();
        }

        [Test]
        public void NoArgs_DumpsAll()
        {
            var result = _command.Execute(_context, []);
            var output = (TestOutput)_context.Output;
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(output.StatusWrites, Has.Count.EqualTo(1));
                Assert.That(output.MemoryDumps, Has.Count.EqualTo(1));
                Assert.That(output.StackDumps, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public void StatusArg_DumpsOnlyStatus()
        {
            var result = _command.Execute(_context, ["status"]);
            var output = (TestOutput)_context.Output;
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(output.StatusWrites, Has.Count.EqualTo(1));
                Assert.That(output.MemoryDumps, Has.Count.EqualTo(0));
                Assert.That(output.StackDumps, Has.Count.EqualTo(0));
            });
        }

        [Test]
        public void InvalidArg_ReturnsError()
        {
            var result = _command.Execute(_context, ["invalid"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void TooManyArgs_ReturnsError()
        {
            var result = _command.Execute(_context, ["status", "memory", "stack", "extra"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Message, Does.Contain("takes at most 3"));
                Assert.That(result.Message, Does.Not.Contain("does takes"));
            });
        }

        [Test]
        public void MultipleArgs_DumpsSelected()
        {
            var result = _command.Execute(_context, ["memory", "stack"]);
            var output = (TestOutput)_context.Output;
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(output.StatusWrites, Has.Count.EqualTo(0));
                Assert.That(output.MemoryDumps, Has.Count.EqualTo(1));
                Assert.That(output.StackDumps, Has.Count.EqualTo(1));
            });
        }
    }
}
