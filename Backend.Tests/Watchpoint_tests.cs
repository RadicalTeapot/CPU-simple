using Backend.Commands;
using Backend.Commands.GlobalCommands;
using WatchpointCommand = Backend.Commands.GlobalCommands.Watchpoint;

namespace Backend.Tests
{
    [TestFixture]
    internal class Watchpoint_tests
    {
        private WatchpointCommand _command = null!;
        private GlobalCommandExecutionContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _command = new WatchpointCommand(new CommandContext("watchpoint", "Add, remove, or list watchpoints", "Usage: 'watchpoint [on-write/on-read <address> | on-phase <phase> | remove <id> | clear | list]'"));
            _context = BackendTestHelpers.CreateGlobalContext();
        }

        [Test]
        public void OnWrite_ValidAddress_AddsWatchpoint()
        {
            var result = _command.Execute(_context, ["on-write", "12"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Watchpoints.Count, Is.EqualTo(1));
                Assert.That(_context.Watchpoints.GetAll()[0].Description, Does.Contain("on-write"));
            });
        }

        [Test]
        public void OnRead_ValidAddress_AddsWatchpoint()
        {
            var result = _command.Execute(_context, ["on-read", "12"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Watchpoints.Count, Is.EqualTo(1));
                Assert.That(_context.Watchpoints.GetAll()[0].Description, Does.Contain("on-read"));
            });
        }

        [Test]
        public void OnPhase_ValidPhase_AddsWatchpoint()
        {
            var result = _command.Execute(_context, ["on-phase", "MemoryWrite"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Watchpoints.Count, Is.EqualTo(1));
                Assert.That(_context.Watchpoints.GetAll()[0].Description, Does.Contain("MemoryWrite"));
            });
        }

        [Test]
        public void OnPhase_CaseInsensitive_AddsWatchpoint()
        {
            var result = _command.Execute(_context, ["on-phase", "memorywrite"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Watchpoints.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void OnPhase_InvalidPhase_ReturnsError()
        {
            var result = _command.Execute(_context, ["on-phase", "InvalidPhase"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Message, Does.Contain("not a valid MicroPhase"));
            });
        }

        [Test]
        public void OnWrite_NoAddress_ReturnsError()
        {
            var result = _command.Execute(_context, ["on-write"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void OnRead_NoAddress_ReturnsError()
        {
            var result = _command.Execute(_context, ["on-read"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void OnWrite_InvalidAddress_ReturnsError()
        {
            var result = _command.Execute(_context, ["on-write", "notanumber"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Remove_ExistingId_RemovesWatchpoint()
        {
            _command.Execute(_context, ["on-write", "12"]);
            var id = _context.Watchpoints.GetAll()[0].Id;
            var result = _command.Execute(_context, ["remove", id.ToString()]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Watchpoints.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void Remove_NoId_ReturnsError()
        {
            var result = _command.Execute(_context, ["remove"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Clear_RemovesAllWatchpoints()
        {
            _command.Execute(_context, ["on-write", "10"]);
            _command.Execute(_context, ["on-read", "20"]);
            var result = _command.Execute(_context, ["clear"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(_context.Watchpoints.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void List_NoWatchpoints_ReportsNone()
        {
            var result = _command.Execute(_context, ["list"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("No watchpoints"));
            });
        }

        [Test]
        public void List_WithWatchpoints_ReportsDescriptions()
        {
            _command.Execute(_context, ["on-write", "12"]);
            var result = _command.Execute(_context, ["list"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("on-write"));
            });
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
            var result = _command.Execute(_context, ["on-write", "1", "extra"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void InvalidAction_ReturnsErrorWithValidActions()
        {
            var result = _command.Execute(_context, ["invalid"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Message, Does.Contain("on-write"));
                Assert.That(result.Message, Does.Contain("on-read"));
                Assert.That(result.Message, Does.Contain("on-phase"));
            });
        }

        [Test]
        public void WriteWatchpointList_CalledAfterMutation()
        {
            _command.Execute(_context, ["on-write", "12"]);
            var output = (TestOutput)_context.Output;
            Assert.That(output.WatchpointLists, Has.Count.EqualTo(1));
            Assert.That(output.WatchpointLists[0], Has.Length.EqualTo(1));
        }
    }
}
