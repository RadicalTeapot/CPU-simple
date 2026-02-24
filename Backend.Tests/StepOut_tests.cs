using Backend.Commands;
using Backend.Commands.StateCommands;
using Backend.CpuStates;
using Backend.IO;
using CPU;

namespace Backend.Tests
{
    [TestFixture]
    internal class StepOut_tests
    {
        private StepOut _command = null!;

        [SetUp]
        public void SetUp()
        {
            _command = new StepOut(new CommandContext("stepout", "Step out of subroutine", "Usage: 'stepout'"));
        }

        private static CpuStateFactory CreateFactory(byte[] program, int stackSize = 16)
        {
            var config = new Config(256, stackSize, 4);
            var cpu = new CPU.CPU(config);
            cpu.LoadProgram(program);
            var logger = new TestLogger();
            var output = new TestOutput();
            var breakpoints = new BreakpointContainer();
            var watchpoints = new WatchpointContainer();
            var registry = new StateCommandRegistry();
            return new CpuStateFactory(cpu, logger, output, breakpoints, watchpoints, registry);
        }

        private static CPU.CPU GetCpu(CpuStateFactory factory)
        {
            // Primary constructor parameters in C# are stored as fields
            var fields = typeof(CpuStateFactory).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(CPU.CPU))
                    return (CPU.CPU)f.GetValue(factory)!;
            }
            throw new InvalidOperationException("Could not find CPU field in CpuStateFactory");
        }

        [Test]
        public void EmptyStack_ReturnsError()
        {
            // NOP program, stack is empty
            var factory = CreateFactory([0x00]); // NOP opcode
            var result = _command.Execute(factory, []);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("stack"));
        }

        [Test]
        public void WithReturnAddress_ReturnsSuccess()
        {
            // CAL = 0x0E, followed by address operand
            // Build: CAL [target], NOP, HLT, <target>: NOP
#if x16
            // CAL(1) + addr(2) = 3 bytes, then NOP(1), HLT(1), target NOP(1)
            // CAL pushes return address (PC after CAL = 3) onto stack as 2 bytes
            byte[] program = [0x0E, 0x05, 0x00, 0x00, 0x01, 0x00]; // CAL [0x0005], NOP, HLT, NOP
#else
            // CAL(1) + addr(1) = 2 bytes, then NOP(1), HLT(1), target NOP(1)
            byte[] program = [0x0E, 0x04, 0x00, 0x01, 0x00]; // CAL [0x04], NOP, HLT, NOP
#endif
            var factory = CreateFactory(program);
            var cpu = GetCpu(factory);
            cpu.Step(); // Execute CAL — pushes return address onto stack

            var result = _command.Execute(factory, []);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Does.Contain("Stepping out"));
        }

#if x16
        [Test]
        public void X16_OnlyOneByteOnStack_ReturnsError()
        {
            // PSH r0 = 0x20 (PSH base) | 0x00 (r0) = 0x20
            // This pushes 1 byte onto stack. In x16 mode, StepOut needs 2 bytes.
            byte[] program = [0x20, 0x01]; // PSH r0, HLT
            var factory = CreateFactory(program, stackSize: 4);
            var cpu = GetCpu(factory);
            cpu.Step(); // Execute PSH r0 - pushes 1 byte

            var result = _command.Execute(factory, []);
            // SP went from 3 to 2, StackContents.Length is 4
            // SP(2) >= Length-2(2) is true, so should return error
            Assert.That(result.Success, Is.False);
        }
#endif
    }
}
