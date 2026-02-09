namespace Backend.Tests
{
    [TestFixture]
    internal class Backend_tests
    {
        private TestLogger _logger = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
        }

        [Test]
        public void ParseArgs_NoArgs_ReturnsDefaultConfig()
        {
            var code = Backend.ParseArgs([], _logger, out var config);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(config.MemorySize, Is.EqualTo(256));
                Assert.That(config.StackSize, Is.EqualTo(16));
                Assert.That(config.RegisterCount, Is.EqualTo(4));
            });
        }

        [Test]
        public void ParseArgs_ValidMemoryShort_SetsMemorySize()
        {
            var code = Backend.ParseArgs(["-m", "512"], _logger, out var config);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(config.MemorySize, Is.EqualTo(512));
            });
        }

        [Test]
        public void ParseArgs_ValidMemoryLong_SetsMemorySize()
        {
            var code = Backend.ParseArgs(["--memory", "1024"], _logger, out var config);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(config.MemorySize, Is.EqualTo(1024));
            });
        }

        [Test]
        public void ParseArgs_ValidStack_SetsStackSize()
        {
            var code = Backend.ParseArgs(["-s", "32"], _logger, out var config);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(config.StackSize, Is.EqualTo(32));
            });
        }

        [Test]
        public void ParseArgs_ValidRegisters_SetsRegisterCount()
        {
            var code = Backend.ParseArgs(["--registers", "8"], _logger, out var config);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(config.RegisterCount, Is.EqualTo(8));
            });
        }

        [Test]
        public void ParseArgs_HelpShort_ReturnsHelpExitCode()
        {
            var code = Backend.ParseArgs(["-h"], _logger, out _);
            Assert.That(code, Is.EqualTo(1));
        }

        [Test]
        public void ParseArgs_HelpLong_ReturnsHelpExitCode()
        {
            var code = Backend.ParseArgs(["--help"], _logger, out _);
            Assert.That(code, Is.EqualTo(1));
        }

        [Test]
        public void ParseArgs_UnknownArg_ReturnsInvalidExitCode()
        {
            var code = Backend.ParseArgs(["--unknown"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_InvalidMemoryValue_ReturnsInvalidExitCode()
        {
            var code = Backend.ParseArgs(["-m", "abc"], _logger, out _);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(2));
                Assert.That(_logger.ErrorMessages, Has.Count.GreaterThan(0));
            });
        }

        [Test]
        public void ParseArgs_MissingMemoryValue_ReturnsInvalidExitCode()
        {
            var code = Backend.ParseArgs(["-m"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_InvalidStackValue_ReturnsInvalidExitCode()
        {
            var code = Backend.ParseArgs(["-s", "notanumber"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_InvalidRegistersValue_ReturnsInvalidExitCode()
        {
            var code = Backend.ParseArgs(["--registers", "xyz"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_MultipleValidArgs_SetsAll()
        {
            var code = Backend.ParseArgs(["-m", "512", "-s", "32", "--registers", "8"], _logger, out var config);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(config.MemorySize, Is.EqualTo(512));
                Assert.That(config.StackSize, Is.EqualTo(32));
                Assert.That(config.RegisterCount, Is.EqualTo(8));
            });
        }
    }
}
