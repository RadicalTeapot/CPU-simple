using Backend.Commands.StateCommands;
using PPU.Configuration;

namespace Backend.Tests
{
    [TestFixture]
    internal class CpuHandler_tests
    {
        [Test]
        public void FrameReady_FiredAfterTickFrame_WhenPpuEnabled()
        {
            var config = new CPU.Config(256, 16, 4, vramSize: 256);
            var handler = CreateHandler(config);
            bool fired = false;
            handler.FrameReady += _ => fired = true;

            handler.TickFrame();

            Assert.That(fired, Is.True);
        }

        [Test]
        public void TickFrame_NoPpu_DoesNotFireFrameReady()
        {
            var config = new CPU.Config(256, 16, 4, vramSize: 0);
            var handler = CreateHandler(config);
            bool fired = false;
            handler.FrameReady += _ => fired = true;

            handler.TickFrame();

            Assert.That(fired, Is.False);
        }

        [Test]
        public void FrameReady_RgbDataLength_MatchesPpuScreenSize()
        {
            var config = new CPU.Config(256, 16, 4, vramSize: 256);
            var handler = CreateHandler(config);
            IReadOnlyList<byte>? rgbData = null;
            handler.FrameReady += rgb => rgbData = rgb;

            handler.TickFrame();

            var ppuConfig = PpuConfig.Minimal8Bit;
            Assert.That(rgbData, Is.Not.Null);
            Assert.That(rgbData!.Count, Is.EqualTo(ppuConfig.ScreenWidth * ppuConfig.ScreenHeight * 3));
        }

        [Test]
        public void FrameReady_FiredOncePerTickFrame()
        {
            var config = new CPU.Config(256, 16, 4, vramSize: 256);
            var handler = CreateHandler(config);
            int frameCount = 0;
            handler.FrameReady += _ => frameCount++;

            handler.TickFrame();

            Assert.That(frameCount, Is.EqualTo(1));
        }

        private static CpuHandler CreateHandler(CPU.Config config)
        {
            return new CpuHandler(config, new TestOutput(), new TestLogger(), new StateCommandRegistry());
        }
    }
}
