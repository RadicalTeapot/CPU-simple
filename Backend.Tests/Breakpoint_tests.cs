using Backend.Commands;
using Backend.Commands.GlobalCommands;
using BreakpointCommand = Backend.Commands.GlobalCommands.Breakpoint;

namespace Backend.Tests
{
    [TestFixture]
    internal class Breakpoint_tests
    {
        private BreakpointCommand _command = null!;
        private GlobalCommandExecutionContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _command = new BreakpointCommand(new CommandContext("breakpoint", "Toggle or remove breakpoint(s)", "Usage: 'breakpoint [toggle/clear/list] [address]'"));
            _context = BackendTestHelpers.CreateGlobalContext();
        }

        [Test]
        public void Toggle_ValidAddress_AddsBreakpoint()
        {
            var result = _command.Execute(_context, ["toggle", "100"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Breakpoints.Contains(100), Is.True);
            });
        }

        [Test]
        public void Toggle_ExistingAddress_RemovesBreakpoint()
        {
            _context.Breakpoints.Add(100);
            var result = _command.Execute(_context, ["toggle", "100"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Breakpoints.Contains(100), Is.False);
            });
        }

        [Test]
        public void Clear_RemovesAllBreakpoints()
        {
            _context.Breakpoints.Add(1);
            _context.Breakpoints.Add(2);
            var result = _command.Execute(_context, ["clear"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Breakpoints.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void List_NoBreakpoints_ReportsNone()
        {
            var result = _command.Execute(_context, ["list"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("No breakpoints"));
            });
        }

        [Test]
        public void List_WithBreakpoints_ReportsAddresses()
        {
            _context.Breakpoints.Add(42);
            var result = _command.Execute(_context, ["list"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("002A")); // 42 in hex
            });
        }

        [Test]
        public void Toggle_WithoutAddress_ReturnsError()
        {
            var result = _command.Execute(_context, ["toggle"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Toggle_InvalidAddress_ReturnsError()
        {
            var result = _command.Execute(_context, ["toggle", "notanumber"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void TooFewArgs_ReturnsError()
        {
            var result = _command.Execute(_context, []);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void TooManyArgs_ReturnsError()
        {
            var result = _command.Execute(_context, ["toggle", "1", "extra"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void InvalidAction_ReturnsErrorWithValidActions()
        {
            var result = _command.Execute(_context, ["invalid"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Message, Does.Contain("toggle"));
                Assert.That(result.Message, Does.Contain("clear"));
                Assert.That(result.Message, Does.Contain("list"));
            });
        }
    }
}
