using PPU.Configuration;
using PPU.Storage;

namespace PPU.Tests
{
    [TestFixture]
    public class Ppu_tests
    {
        [Test]
        public void Tick_ReturnsTickResult()
        {
            var config = PpuTestHelpers.CreateTickConfig();
            var rom = PpuTestHelpers.CreateChrRom(config);
            var ppu = new Ppu(config, rom);
            var result = ppu.Tick();
            Assert.That(result, Is.InstanceOf<PPU.DebuggerInteraction.PpuTickResult>());
        }

        [Test]
        public void Registers_ReturnsIMmioDevice()
        {
            var config = PpuTestHelpers.CreateTickConfig();
            var rom = PpuTestHelpers.CreateChrRom(config);
            var ppu = new Ppu(config, rom);
            Assert.That(ppu.Registers, Is.Not.Null);
        }

        [Test]
        public void Tick_FullFrame_FiresVBlankEvent()
        {
            // CreateTickConfig: TilemapHeight=1, TileSize=8 → ScreenHeight=8
            // CyclesPerScanline=1, VBlankScanlineCount=2
            // Each Ppu.Tick increments _scanlineCycle to 1 >= CyclesPerScanline(1),
            // resets and calls state.Tick().
            // RenderState.Tick: checks scanline==ScreenHeight first, then renders+increments.
            // Ticks 1-8: render scanlines 0-7.
            // Tick 9: scanline=8==ScreenHeight → VBlank.Enter + VBlank.Tick
            var config = PpuTestHelpers.CreateTickConfig();
            var rom = PpuTestHelpers.CreateChrRom(config);
            var ppu = new Ppu(config, rom);
            var vblankFired = false;
            ppu.VBlankStarted += () => vblankFired = true;
            int renderTicks = config.ScreenHeight; // 8

            // 8 ticks render scanlines 0-7, scanline counter reaches 8
            for (int i = 0; i < renderTicks; i++)
                ppu.Tick();

            Assert.That(vblankFired, Is.False);
            // 9th tick: scanline=8==ScreenHeight → transitions to VBlank, fires event
            ppu.Tick();
            Assert.That(vblankFired, Is.True);
        }

        [Test]
        public void Tick_VBlank_SetsVBlankActive()
        {
            var config = PpuTestHelpers.CreateTickConfig();
            var rom = PpuTestHelpers.CreateChrRom(config);
            var ppu = new Ppu(config, rom);

            // Tick through render + into VBlank
            for (int i = 0; i < config.ScreenHeight + 1; i++)
                ppu.Tick();

            // VBlankActive should be set via the registers
            var status = ppu.Registers.ReadRegister(2);
            Assert.That(status & 0x80, Is.EqualTo(0x80));
        }

        [Test]
        public void Tick_VBlankDuration_LastsConfiguredScanlines()
        {
            // ScreenHeight=8, VBlankScanlineCount=2, CyclesPerScanline=1
            // Ticks 1-8: render scanlines 0-7.
            // Tick 9: scanline=8==ScreenHeight → VBlank.Enter (fires event, _scanline=0) + VBlank.Tick
            // Tick 10: VBlank.Tick, _scanline=1 < 2, _scanline→2.
            // Tick 11: scanline=2==VBlankScanlineCount, → render.Enter (fires event, _scanline=0) + render.Tick
            // ...

            var config = PpuTestHelpers.CreateTickConfig();
            var rom = PpuTestHelpers.CreateChrRom(config);
            var ppu = new Ppu(config, rom);
            var vblankCount = 0;
            ppu.VBlankStarted += () => vblankCount++;

            // Complete first full frame cycle (render + vblank transition + vblank ticks including exit)
            // 8 render + 2 vblank ticks (2 stay) = 10
            int renderTicks = config.ScreenHeight; // 8
            int vblankTicks = config.VBlankScanlineCount; // 2
            int totalFirstFrame = renderTicks + vblankTicks;
            for (int i = 0; i < totalFirstFrame; i++)
                ppu.Tick();

            // After 10 ticks, the PPU completed the first frame and should have fired VBlankStarted once
            Assert.That(vblankCount, Is.EqualTo(1));

            // Second frame: render + first vblank tick
            for (int i = 0; i < renderTicks + 1; i++)
                ppu.Tick();

            Assert.That(vblankCount, Is.EqualTo(2));
        }

        [Test]
        public void Tick_FullCycle_ReturnsToRendering()
        {
            var config = PpuTestHelpers.CreateTickConfig();
            var rom = PpuTestHelpers.CreateChrRom(config);
            var ppu = new Ppu(config, rom);
            var vblankCount = 0;
            ppu.VBlankStarted += () => vblankCount++;

            // Full first frame: render(8) + vblank entry(1) + vblank ticks(VBlankScanlineCount+1)
            int renderTicks = config.ScreenHeight;
            int vblankEntryTick = 1;
            int vblankTicks = config.VBlankScanlineCount + 1;
            int totalFirstFrame = renderTicks + vblankEntryTick + vblankTicks;
            for (int i = 0; i < totalFirstFrame; i++)
                ppu.Tick();

            Assert.That(vblankCount, Is.EqualTo(1));

            // Second frame: render + vblank entry
            for (int i = 0; i < renderTicks + vblankEntryTick; i++)
                ppu.Tick();

            Assert.That(vblankCount, Is.EqualTo(2));
        }

        [Test]
        public void VBlankStarted_FiredOnlyOncePerFrame()
        {
            var config = PpuTestHelpers.CreateTickConfig();
            var rom = PpuTestHelpers.CreateChrRom(config);
            var ppu = new Ppu(config, rom);
            var vblankCount = 0;
            ppu.VBlankStarted += () => vblankCount++;

            // Render phase + VBlank entry + all VBlank scanlines
            int totalFirstFrame = config.ScreenHeight + 1 + config.VBlankScanlineCount;
            for (int i = 0; i < totalFirstFrame; i++)
                ppu.Tick();

            Assert.That(vblankCount, Is.EqualTo(1));
        }
    }
}
