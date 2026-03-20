using Controller.Configuration;
using Emulator.Commands.StateCommands;
using PPU.Configuration;
using Emulator;

namespace Emulator.Tests
{
    [TestFixture]
    internal class CpuHandler_tests
    {
        [Test]
        public void FrameReady_FiredAfterTickFrame_WhenPpuEnabled()
        {
            var config = new CPU.Config(256, 16, 4, vramSize: 0);
            var handler = CreateHandler(config, PpuConfig.Minimal8Bit);
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
            var config = new CPU.Config(256, 16, 4, vramSize: 0);
            var handler = CreateHandler(config, PpuConfig.Minimal8Bit);
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
            var config = new CPU.Config(256, 16, 4, vramSize: 0);
            var handler = CreateHandler(config, PpuConfig.Minimal8Bit);
            int frameCount = 0;
            handler.FrameReady += _ => frameCount++;

            handler.TickFrame();

            Assert.That(frameCount, Is.EqualTo(1));
        }

        [Test]
        public void FrameReady_WithChrData_RendersCorrectPixels()
        {
            // All-0xFF CHR data → all tile rows fully lit → all pixels white → all RGB bytes 255
            var ppuConfig = PpuConfig.Minimal8Bit;
            var chrData = new byte[ppuConfig.BytesPerTile * ppuConfig.ChrCount];
            Array.Fill(chrData, (byte)0xFF);
            var config = new CPU.Config(256, 16, 4, vramSize: 0);
            var handler = CreateHandler(config, ppuConfig, chrData);
            IReadOnlyList<byte>? rgbData = null;
            handler.FrameReady += rgb => rgbData = rgb;

            handler.TickFrame();

            Assert.That(rgbData, Is.Not.Null);
            Assert.That(rgbData!.All(b => b == 255), Is.True);
        }

        [Test]
        public void FrameReady_FiredAfterTickFrame_WithControllerAndPpu()
        {
            var ppuConfig = PpuConfig.Minimal8Bit;
            var controllerConfig = new ControllerConfiguration(ButtonCount: 2);
            var peripherals = new PeripheralSet(ppuConfig, ChrData: null, controllerConfig);
            var config = new CPU.Config(256, 16, 4, vramSize: 0);
            var context = new CpuHandler.CpuHandlerContext(
                config, new TestLogger(), new TestOutput(), new StateCommandRegistry(),
                peripherals
            );
            var handler = new CpuHandler(context);
            bool fired = false;
            handler.FrameReady += _ => fired = true;

            handler.TickFrame();

            Assert.That(fired, Is.True);
        }

        private static CpuHandler CreateHandler(CPU.Config config, PpuConfig? ppuConfig = null, IReadOnlyList<byte>? chrData = null, IReadOnlyList<byte>? progData = null)
        {
            var peripherals = ppuConfig != null ? new PeripheralSet(ppuConfig, chrData) : null;
            var context = new CpuHandler.CpuHandlerContext(
                config,
                new TestLogger(),
                new TestOutput(),
                new StateCommandRegistry(),
                peripherals,
                progData
            );
            return new CpuHandler(context);
        }
    }
}
