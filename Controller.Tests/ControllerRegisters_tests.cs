using Controller.Configuration;
using Controller.Exceptions;
using Controller.Storage;

namespace Controller.Tests
{
    [TestFixture]
    public class ControllerRegisters_tests
    {
        // Status register (offset 0) - direction bits

        [Test]
        public void ReadStatus_UpPressed_ReturnsBit0Set()
        {
            var (regs, state) = CreateRegisters();
            state.SetUp();
            Assert.That(regs.ReadRegister(0) & 0x01, Is.EqualTo(0x01));
        }

        [Test]
        public void ReadStatus_DownPressed_ReturnsBit1Set()
        {
            var (regs, state) = CreateRegisters();
            state.SetDown();
            Assert.That(regs.ReadRegister(0) & 0x02, Is.EqualTo(0x02));
        }

        [Test]
        public void ReadStatus_LeftPressed_ReturnsBit2Set()
        {
            var (regs, state) = CreateRegisters();
            state.SetLeft();
            Assert.That(regs.ReadRegister(0) & 0x04, Is.EqualTo(0x04));
        }

        [Test]
        public void ReadStatus_RightPressed_ReturnsBit3Set()
        {
            var (regs, state) = CreateRegisters();
            state.SetRight();
            Assert.That(regs.ReadRegister(0) & 0x08, Is.EqualTo(0x08));
        }

        [TestCase(0, 0x10)]
        [TestCase(1, 0x20)]
        [TestCase(2, 0x40)]
        [TestCase(3, 0x80)]
        public void ReadStatus_ButtonPressed_ReturnsBit4PlusIndexSet(int buttonIndex, int expectedBit)
        {
            var (regs, state) = CreateRegisters(4);
            state.SetButton(buttonIndex);
            Assert.That(regs.ReadRegister(0) & expectedBit, Is.EqualTo(expectedBit));
        }

        [Test]
        public void ReadStatus_NoButtonsPressed_ReturnsZero()
        {
            var (regs, _) = CreateRegisters();
            Assert.That(regs.ReadRegister(0), Is.EqualTo(0));
        }

        [Test]
        public void ReadStatus_OnlyPressedButtonsSetInByte()
        {
            var (regs, state) = CreateRegisters(4);
            state.SetUp();
            state.SetButton(1);
            var status = regs.ReadRegister(0);
            Assert.Multiple(() =>
            {
                Assert.That(status & 0x01, Is.EqualTo(0x01)); // Up
                Assert.That(status & 0x02, Is.EqualTo(0x00)); // Down not set
                Assert.That(status & 0x20, Is.EqualTo(0x20)); // Button 1
                Assert.That(status & 0x10, Is.EqualTo(0x00)); // Button 0 not set
            });
        }

        // State cleared after read

        [Test]
        public void ReadStatus_ClearsDirectionsAfterRead()
        {
            var (regs, state) = CreateRegisters();
            state.SetUp();
            regs.ReadRegister(0);
            Assert.That(regs.ReadRegister(0), Is.EqualTo(0));
        }

        [Test]
        public void ReadStatus_ClearsCustomButtonsAfterRead()
        {
            var (regs, state) = CreateRegisters(2);
            state.SetButton(0);
            regs.ReadRegister(0);
            Assert.That(regs.ReadRegister(0), Is.EqualTo(0));
        }

        [Test]
        public void ReadStatus_UnpressedButtonNotSet()
        {
            var (regs, _) = CreateRegisters(2);
            Assert.That(regs.ReadRegister(0) & 0x20, Is.EqualTo(0));
        }

        // Unknown offsets and writes

        [Test]
        public void ReadRegister_UnknownOffset_ReturnsZero()
        {
            var (regs, _) = CreateRegisters();
            Assert.That(regs.ReadRegister(1), Is.EqualTo(0));
        }

        [Test]
        public void WriteRegister_DoesNotThrow()
        {
            var (regs, _) = CreateRegisters();
            Assert.That(() => regs.WriteRegister(0, 0xFF), Throws.Nothing);
        }

        private static (ControllerRegisters regs, ButtonsState state) CreateRegisters(int buttonCount = 0)
        {
            var config = new ControllerConfiguration(buttonCount);
            var state = new ButtonsState(config);
            return (new ControllerRegisters(state), state);
        }
    }
}
