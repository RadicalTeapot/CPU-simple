using PPU.Configuration;
using PPU.Rendering;

namespace PPU.Tests
{
    [TestFixture]
    public class Framebuffer_tests
    {
        [Test]
        public void AsReadOnly_InitialState_AllZeros()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var fb = new Framebuffer(config);
            var pixels = fb.AsReadOnly();
            Assert.That(pixels.All(p => p == 0), Is.True);
        }

        [Test]
        public void AsReadOnly_Length_MatchesScreenSize()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var fb = new Framebuffer(config);
            Assert.That(fb.AsReadOnly().Count, Is.EqualTo(config.ScreenWidth * config.ScreenHeight));
        }

        [Test]
        public void Write_RowAndCol_CorrectIndex()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var fb = new Framebuffer(config);
            fb.Write(1, 5, 0x42);
            var pixels = fb.AsReadOnly();
            Assert.That(pixels[1 * config.ScreenWidth + 5], Is.EqualTo(0x42));
        }

        [Test]
        public void Clear_AfterWrite_AllZeros()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var fb = new Framebuffer(config);
            fb.Write(0, 0, 0xFF);
            fb.Clear();
            var pixels = fb.AsReadOnly();
            Assert.That(pixels.All(p => p == 0), Is.True);
        }
    }
}
