using Backend.Commands;
using Backend.Commands.GlobalCommands;

namespace Backend.Tests
{
    [TestFixture]
    internal class ReadMemory_tests
    {
        private ReadMemory _command = null!;
        private GlobalCommandExecutionContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _command = new ReadMemory(new CommandContext("readmem", "Reads parts or whole memory", "Usage: 'readmem [startaddress [length]]'"));
            _context = BackendTestHelpers.CreateGlobalContext();
        }

        [Test]
        public void NoArgs_ReadsFullMemory()
        {
            var result = _command.Execute(_context, []);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("[MEMORY]"));
            });
        }

        [Test]
        public void AddressAndLength_ReadsRange()
        {
            var result = _command.Execute(_context, ["00", "4"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("[MEMORY]"));
            });
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
            // Memory size is 256-16=240, so address 240 is out of bounds
            var result = _command.Execute(_context, ["FFFF"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void NegativeAddress_ReturnsError()
        {
            // Convert.ToInt32 with base 16 can produce negative from large values
            var result = _command.Execute(_context, ["-1"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void ValidAddressWithinBounds_ReturnsSuccess()
        {
            var result = _command.Execute(_context, ["00", "10"]);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void LengthExceedsRemaining_ClampedToAvailable()
        {
            // Address 0, request 1000 bytes but only 240 available
            var result = _command.Execute(_context, ["00", "1000"]);
            Assert.That(result.Success, Is.True);
        }
    }
}
