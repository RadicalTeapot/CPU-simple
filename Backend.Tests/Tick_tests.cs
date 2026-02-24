using Backend.Commands;
using Backend.Commands.StateCommands;

namespace Backend.Tests
{
    [TestFixture]
    internal class Tick_tests
    {
        private Tick _command = null!;

        [SetUp]
        public void SetUp()
        {
            _command = new Tick(new CommandContext("tick", "Tick the CPU for the specified number of micro-ticks.", "Usage: 'tick [count]'"));
        }

        [Test]
        public void NoArgs_DefaultsToOneTick()
        {
            var factory = CreateFactory([0x00]); // NOP
            var result = _command.Execute(factory, []);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.NextState, Is.TypeOf<CpuStates.TickingState>());
                Assert.That(result.Message, Does.Contain("1"));
            });
        }

        [Test]
        public void ValidCount_ReturnsTickingState()
        {
            var factory = CreateFactory([0x00]);
            var result = _command.Execute(factory, ["5"]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.NextState, Is.TypeOf<CpuStates.TickingState>());
                Assert.That(result.Message, Does.Contain("5"));
            });
        }

        [Test]
        public void InvalidCount_ReturnsError()
        {
            var factory = CreateFactory([0x00]);
            var result = _command.Execute(factory, ["notanumber"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void ZeroCount_ReturnsError()
        {
            var factory = CreateFactory([0x00]);
            var result = _command.Execute(factory, ["0"]);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void NegativeCount_ReturnsError()
        {
            var factory = CreateFactory([0x00]);
            var result = _command.Execute(factory, ["-1"]);
            Assert.That(result.Success, Is.False);
        }

        private static CpuStates.CpuStateFactory CreateFactory(byte[] program)
        {
            var config = new CPU.Config(256, 16, 4);
            var cpu = new CPU.CPU(config);
            cpu.LoadProgram(program);
            var logger = new TestLogger();
            var output = new TestOutput();
            var breakpoints = new BreakpointContainer();
            var watchpoints = new WatchpointContainer();
            var registry = new StateCommandRegistry();
            return new CpuStates.CpuStateFactory(cpu, logger, output, breakpoints, watchpoints, registry);
        }
    }
}
