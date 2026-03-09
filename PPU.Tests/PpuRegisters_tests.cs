using PPU.Configuration;
using PPU.Storage;

namespace PPU.Tests
{
    [TestFixture]
    public class PpuRegisters_tests
    {
        // Status reads (offset 2)

        [Test]
        public void ReadStatus_VBlankActive_ReturnsBit7Set()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.VBlankActive = true;
            var status = regs.ReadRegister(2);
            Assert.That(status & 0x80, Is.EqualTo(0x80));
        }

        [Test]
        public void ReadStatus_VBlankNotActive_ReturnsBit7Clear()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.VBlankActive = false;
            var status = regs.ReadRegister(2);
            Assert.That(status & 0x80, Is.EqualTo(0));
        }

        [Test]
        public void ReadStatus_SpriteOverflow_ReturnsBit6Set()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.SpriteOverflow = true;
            var status = regs.ReadRegister(2);
            Assert.That(status & 0x40, Is.EqualTo(0x40));
        }

        [Test]
        public void ReadStatus_ClearsVBlankActive()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.VBlankActive = true;
            regs.ReadRegister(2);
            Assert.That(regs.VBlankActive, Is.False);
        }

        [Test]
        public void ReadStatus_DoesNotClearSpriteOverflow()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.SpriteOverflow = true;
            regs.ReadRegister(2);
            Assert.That(regs.SpriteOverflow, Is.True);
        }

        [Test]
        public void ReadStatus_ResetsAddressLatch()
        {
            var config = PpuTestHelpers.CreateLargeVramConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Write high byte of address
            regs.WriteRegister(0, 0x01);
            // Read status to reset latch
            regs.ReadRegister(2);
            // Next write should be treated as high byte again
            regs.WriteRegister(0, 0x00);
            regs.WriteRegister(0, 0x05); // low byte
            regs.WriteRegister(1, 0xAA); // write data at address 0x0005
            Assert.That(vram.Read(5), Is.EqualTo(0xAA));
        }

        // Small VRAM (<=256, single-byte address)

        [Test]
        public void WriteAddr_SmallVram_SingleByteAddress()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.WriteRegister(0, 10); // set address to 10
            regs.WriteRegister(1, 0x42); // write data
            Assert.That(vram.Read(10), Is.EqualTo(0x42));
        }

        [Test]
        public void WriteData_AutoIncrements()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.WriteRegister(0, 0); // set address to 0
            regs.WriteRegister(1, 0xAA); // write to addr 0
            regs.WriteRegister(1, 0xBB); // write to addr 1 (auto-incremented)
            Assert.Multiple(() =>
            {
                Assert.That(vram.Read(0), Is.EqualTo(0xAA));
                Assert.That(vram.Read(1), Is.EqualTo(0xBB));
            });
        }

        [Test]
        public void WriteData_OutOfBounds_SilentlyIgnored()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.WriteRegister(0, 0xFF); // set address to 255 (last valid for 256-byte VRAM)
            regs.WriteRegister(1, 0x01); // write at 255 - ok
            Assert.That(() => regs.WriteRegister(1, 0x02), Throws.Nothing); // write at 256 - out of bounds, silently ignored
        }

        // Large VRAM (>256, dual-latch)

        [Test]
        public void WriteAddr_DualLatch_HighThenLow()
        {
            var config = PpuTestHelpers.CreateLargeVramConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.WriteRegister(0, 0x01); // high byte
            regs.WriteRegister(0, 0x00); // low byte → address 0x0100
            regs.WriteRegister(1, 0x42); // write data
            Assert.That(vram.Read(0x0100), Is.EqualTo(0x42));
        }

        [Test]
        public void WriteAddr_DualLatch_StatusResetsLatch()
        {
            var config = PpuTestHelpers.CreateLargeVramConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.WriteRegister(0, 0x01); // high byte - latch now expects low
            regs.ReadRegister(2); // read status resets latch to high
            regs.WriteRegister(0, 0x00); // this is now high byte (not low)
            regs.WriteRegister(0, 0x05); // low byte → address 0x0005
            regs.WriteRegister(1, 0xBB);
            Assert.That(vram.Read(5), Is.EqualTo(0xBB));
        }

        [Test]
        public void WriteData_DualLatch_AutoIncrements()
        {
            var config = PpuTestHelpers.CreateLargeVramConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.WriteRegister(0, 0x00); // high byte
            regs.WriteRegister(0, 0x0A); // low byte → address 0x000A
            regs.WriteRegister(1, 0xCC); // write at 0x0A
            regs.WriteRegister(1, 0xDD); // write at 0x0B
            Assert.Multiple(() =>
            {
                Assert.That(vram.Read(0x0A), Is.EqualTo(0xCC));
                Assert.That(vram.Read(0x0B), Is.EqualTo(0xDD));
            });
        }

        // Edge cases

        [Test]
        public void ReadRegister_UnknownOffset_ReturnsZero()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            Assert.That(regs.ReadRegister(5), Is.EqualTo(0));
        }

        [Test]
        public void WriteRegister_UnknownOffset_DoesNotThrow()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            Assert.That(() => regs.WriteRegister(5, 0xFF), Throws.Nothing);
        }
    }
}
