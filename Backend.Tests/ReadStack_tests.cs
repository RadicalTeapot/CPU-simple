using Backend.Commands;
using Backend.Commands.GlobalCommands;

namespace Backend.Tests
{
    [TestFixture]
    internal class ReadStack_tests
    {
        private ReadStack _command = null!;
        private GlobalCommandExecutionContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _command = new ReadStack(new CommandContext("readstack", "Reads parts or whole stack", "Usage: 'readstack [startaddress [length]]'"));
            _context = BackendTestHelpers.CreateGlobalContext();
        }

        [Test]
        public void NoArgs_ReadsFromSP()
        {
            var result = _command.Execute(_context, []);
            // SP starts at top (stackSize - 1), stack is empty so length will be clamped
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AddressAndLength_ReadsRange()
        {
            // SP is at top of stack (15 for size 16), read from top with length 1
            var result = _command.Execute(_context, ["0F", "1"]);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void TooManyArgs_ReturnsError()
        {
            var result = _command.Execute(_context, ["00", "4", "extra"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void NonHexAddress_ReturnsError()
        {
            var result = _command.Execute(_context, ["notahex"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void AddressOutOfBounds_ReturnsError()
        {
            var result = _command.Execute(_context, ["FFFF", "1"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void NegativeAddress_ReturnsError()
        {
            var result = _command.Execute(_context, ["-1"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void ValidAddress_ReturnsSuccess()
        {
            var result = _command.Execute(_context, ["0F", "16"]);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void ZeroLengthOrEmptyStack_ReturnsError()
        {
            // Address 0 with length clamped to min(length, address+1) = min(1, 1) = 1, should succeed
            var result = _command.Execute(_context, ["00", "0"]);
            Assert.That(result.Success, Is.False);
        }
    }
}
