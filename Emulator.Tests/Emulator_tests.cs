namespace Emulator.Tests
{
    [TestFixture]
    internal class Emulator_tests
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
            var code = Emulator.ParseArgs([], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.MemorySize, Is.EqualTo(256));
                Assert.That(result.Config.StackSize, Is.EqualTo(16));
                Assert.That(result.Config.RegisterCount, Is.EqualTo(4));
            });
        }

        [Test]
        public void ParseArgs_ValidMemoryShort_SetsMemorySize()
        {
            var code = Emulator.ParseArgs(["-m", "512"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.MemorySize, Is.EqualTo(512));
            });
        }

        [Test]
        public void ParseArgs_ValidMemoryLong_SetsMemorySize()
        {
            var code = Emulator.ParseArgs(["--memory", "1024"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.MemorySize, Is.EqualTo(1024));
            });
        }

        [Test]
        public void ParseArgs_ValidStack_SetsStackSize()
        {
            var code = Emulator.ParseArgs(["-s", "32"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.StackSize, Is.EqualTo(32));
            });
        }

        [Test]
        public void ParseArgs_ValidRegisters_SetsRegisterCount()
        {
            var code = Emulator.ParseArgs(["--registers", "8"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.RegisterCount, Is.EqualTo(8));
            });
        }

        [Test]
        public void ParseArgs_HelpShort_ReturnsHelpExitCode()
        {
            var code = Emulator.ParseArgs(["-h"], _logger, out _);
            Assert.That(code, Is.EqualTo(1));
        }

        [Test]
        public void ParseArgs_HelpLong_ReturnsHelpExitCode()
        {
            var code = Emulator.ParseArgs(["--help"], _logger, out _);
            Assert.That(code, Is.EqualTo(1));
        }

        [Test]
        public void ParseArgs_UnknownArg_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["--unknown"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_InvalidMemoryValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["-m", "abc"], _logger, out _);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(2));
                Assert.That(_logger.ErrorMessages, Has.Count.GreaterThan(0));
            });
        }

        [Test]
        public void ParseArgs_MissingMemoryValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["-m"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_InvalidStackValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["-s", "notanumber"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_InvalidRegistersValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["--registers", "xyz"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_MultipleValidArgs_SetsAll()
        {
            var code = Emulator.ParseArgs(["-m", "512", "-s", "32", "--registers", "8"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.MemorySize, Is.EqualTo(512));
                Assert.That(result.Config.StackSize, Is.EqualTo(32));
                Assert.That(result.Config.RegisterCount, Is.EqualTo(8));
            });
        }

        [Test]
        public void ParseArgs_DefaultScale_IsFour()
        {
            var code = Emulator.ParseArgs([], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Scale, Is.EqualTo(4));
            });
        }

        [Test]
        public void ParseArgs_ValidScale_SetsScale()
        {
            var code = Emulator.ParseArgs(["--scale", "2"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Scale, Is.EqualTo(2));
            });
        }

        [Test]
        public void ParseArgs_InvalidScaleValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["--scale", "big"], _logger, out _);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(2));
                Assert.That(_logger.ErrorMessages, Has.Count.GreaterThan(0));
            });
        }

        [Test]
        public void ParseArgs_MissingScaleValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["--scale"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_ValidChr_SetsChrPath()
        {
            var code = Emulator.ParseArgs(["--chr", "/some/file.chr"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.ChrPath, Is.EqualTo("/some/file.chr"));
            });
        }

        [Test]
        public void ParseArgs_MissingChrValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["--chr"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_NoChr_ChrPathIsNull()
        {
            var code = Emulator.ParseArgs([], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.ChrPath, Is.Null);
            });
        }

        [Test]
        public void ParseArgs_ValidButtons_SetsButtonCount()
        {
            var code = Emulator.ParseArgs(["--buttons", "2"], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.ButtonCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void ParseArgs_DefaultButtons_IsNull()
        {
            var code = Emulator.ParseArgs([], _logger, out var result);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.ButtonCount, Is.Null);
            });
        }

        [Test]
        public void ParseArgs_InvalidButtonsValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["--buttons", "abc"], _logger, out _);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(2));
                Assert.That(_logger.ErrorMessages, Has.Count.GreaterThan(0));
            });
        }

        [Test]
        public void ParseArgs_MissingButtonsValue_ReturnsInvalidExitCode()
        {
            var code = Emulator.ParseArgs(["--buttons"], _logger, out _);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void ParseArgs_WithConfigFile_UsesConfigValues()
        {
            var configFile = new Emulator.EmulatorConfig(Memory: 128, Stack: 8, Registers: 2);
            var code = Emulator.ParseArgs([], _logger, out var result, configFile);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.MemorySize, Is.EqualTo(128));
                Assert.That(result.Config.StackSize, Is.EqualTo(8));
                Assert.That(result.Config.RegisterCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void ParseArgs_CliTakesPrecedenceOverConfigFile()
        {
            var configFile = new Emulator.EmulatorConfig(Memory: 128);
            var code = Emulator.ParseArgs(["-m", "200"], _logger, out var result, configFile);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.Config.MemorySize, Is.EqualTo(200));
            });
        }

        [Test]
        public void ParseArgs_ConfigFileButtonsUsedWhenCliNotSet()
        {
            var configFile = new Emulator.EmulatorConfig(Buttons: 3);
            var code = Emulator.ParseArgs([], _logger, out var result, configFile);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.ButtonCount, Is.EqualTo(3));
            });
        }

        [Test]
        public void ParseArgs_CliButtonsTakePrecedenceOverConfigFile()
        {
            var configFile = new Emulator.EmulatorConfig(Buttons: 3);
            var code = Emulator.ParseArgs(["--buttons", "1"], _logger, out var result, configFile);
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(0));
                Assert.That(result.ButtonCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void ValidateArgs_ButtonsWithoutVram_ReturnsFalse()
        {
            var args = new Emulator.ParsedArgsResult(
                new CPU.Config(256, 16, 4, vramSize: 0),
                Scale: 4,
                ChrPath: null,
                ProgPath: null,
                ButtonCount: 2
            );
            var result = Emulator.ValidateArgs(args, _logger);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(_logger.ErrorMessages, Has.Count.GreaterThan(0));
            });
        }

        [Test]
        public void ValidateArgs_ButtonsWithVram_ReturnsTrue()
        {
            var args = new Emulator.ParsedArgsResult(
                new CPU.Config(256, 16, 4, vramSize: 256),
                Scale: 4,
                ChrPath: null,
                ProgPath: null,
                ButtonCount: 2
            );
            var result = Emulator.ValidateArgs(args, _logger);
            Assert.That(result, Is.True);
        }
    }
}
