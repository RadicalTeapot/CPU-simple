using PPU.Configuration;

namespace PPU.Tests
{
    [TestFixture]
    public class PpuConfig_tests
    {
        [Test]
        public void BytesPerTile_1Bpp_Returns8()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            Assert.That(config.BytesPerTile, Is.EqualTo(8));
        }

        [Test]
        public void BytesPerTile_2Bpp_Returns16()
        {
            var config = PpuConfig.Minimal8Bit with { BitsPerPixel = 2 };
            Assert.That(config.BytesPerTile, Is.EqualTo(16));
        }

        [Test]
        public void ScreenWidth_Minimal8Bit_Returns128()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            Assert.That(config.ScreenWidth, Is.EqualTo(128));
        }

        [Test]
        public void ScreenHeight_Minimal8Bit_Returns104()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            Assert.That(config.ScreenHeight, Is.EqualTo(104));
        }

        [Test]
        public void TotalScanlines_Minimal8Bit_Returns128()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            Assert.That(config.TotalScanlines, Is.EqualTo(128));
        }

        [Test]
        public void Minimal8Bit_HasExpectedValues()
        {
            var config = PpuConfig.Minimal8Bit;
            Assert.Multiple(() =>
            {
                Assert.That(config.TilemapWidth, Is.EqualTo(16));
                Assert.That(config.TilemapHeight, Is.EqualTo(13));
                Assert.That(config.ColorMapCount, Is.EqualTo(0));
                Assert.That(config.BitsPerPixel, Is.EqualTo(1));
                Assert.That(config.TileSize, Is.EqualTo(8));
                Assert.That(config.ChrCount, Is.EqualTo(256));
                Assert.That(config.ChrInRom, Is.True);
                Assert.That(config.SpriteCount, Is.EqualTo(16));
                Assert.That(config.BytesPerSprite, Is.EqualTo(3));
                Assert.That(config.MaxSpritesPerScanline, Is.EqualTo(8));
            });
        }

        [Test]
        public void VramLayout_CalledTwice_ReturnsSameInstance()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var layout1 = config.VramLayout;
            var layout2 = config.VramLayout;
            Assert.That(layout1, Is.SameAs(layout2));
        }

        [Test]
        public void VramLayout_ChrInRom_PassesZeroChrCount()
        {
            var config = PpuConfig.Minimal8Bit;
            Assert.That(config.ChrInRom, Is.True);
            // When ChrInRom, tilemap base = 0 (no CHR space in VRAM)
            Assert.That(config.VramLayout.TilemapBase, Is.EqualTo(0));
        }
    }
}
