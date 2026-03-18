using PPU.Configuration;
using PPU.Storage;

namespace PPU.Tests
{
    [TestFixture]
    public class Vram_tests
    {
        [Test]
        public void Size_Minimal8Bit_MatchesLayoutTotalSize()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            Assert.That(vram.Size, Is.EqualTo(config.VramLayout.TotalSize));
        }

        [Test]
        public void Write_ThenRead_ReturnsSameValue()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            vram.Write(0, 0x42);
            Assert.That(vram.Read(0), Is.EqualTo(0x42));
        }

        [Test]
        public void Read_DefaultValue_ReturnsZero()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            Assert.That(vram.Read(0), Is.EqualTo(0));
        }

        [Test]
        public void Read_NegativeAddress_Throws()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            Assert.That(() => vram.Read(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Read_BeyondSize_Throws()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            Assert.That(() => vram.Read(vram.Size), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Write_NegativeAddress_Throws()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            Assert.That(() => vram.Write(-1, 0x00), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Write_BeyondSize_Throws()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            Assert.That(() => vram.Write(vram.Size, 0x00), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Write_LastValidAddress_Succeeds()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var vram = new Vram(config);
            Assert.That(() => vram.Write(vram.Size - 1, 0xFF), Throws.Nothing);
            Assert.That(vram.Read(vram.Size - 1), Is.EqualTo(0xFF));
        }
    }
}
