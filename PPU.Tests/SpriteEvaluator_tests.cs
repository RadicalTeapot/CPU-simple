using PPU.Configuration;
using PPU.Rendering;
using PPU.Storage;

namespace PPU.Tests
{
    [TestFixture]
    public class SpriteEvaluator_tests
    {
        [Test]
        public void Evaluate_SpriteOnScanline_ReturnsIt()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Y=1 → scanline range [8, 16). Evaluate at scanline 10.
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x10, tileIndex: 1, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(10, regs);
            Assert.That(sprites, Has.Length.EqualTo(1));
            Assert.That(sprites[0].TileIndex, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_SpriteAboveScanline_NotReturned()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Y=2 → scanline range [16, 24). Evaluate at scanline 5.
            // Write both sprites to Y=2 so the default-zero sprite 1 doesn't match
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x20, tileIndex: 1, attributes: 0);
            PpuTestHelpers.WriteOamEntry(vram, config, 1, position: 0x20, tileIndex: 2, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(5, regs);
            Assert.That(sprites, Is.Empty);
        }

        [Test]
        public void Evaluate_SpriteBelowScanline_NotReturned()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Y=0 → scanline range [0, 8). Evaluate at scanline 10.
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x00, tileIndex: 1, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(10, regs);
            Assert.That(sprites, Is.Empty);
        }

        [Test]
        public void Evaluate_ExactStartScanline_Returned()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Y=1 → scanline start = 8
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x10, tileIndex: 1, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(8, regs);
            Assert.That(sprites, Has.Length.EqualTo(1));
        }

        [Test]
        public void Evaluate_ExactLastScanline_Returned()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Y=1 → scanline range [8, 16). Last valid = 15.
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x10, tileIndex: 1, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(15, regs);
            Assert.That(sprites, Has.Length.EqualTo(1));
        }

        [Test]
        public void Evaluate_OneAfterLastScanline_NotReturned()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Y=1 → scanline range [8, 16). Scanline 16 is out.
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x10, tileIndex: 1, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(16, regs);
            Assert.That(sprites, Is.Empty);
        }

        [Test]
        public void Evaluate_OverflowDetected_SetsSpriteOverflow()
        {
            // SmallConfig: MaxSpritesPerScanline=2, SpriteCount=2
            // Need 3 sprites on same scanline → use config with SpriteCount=3
            var config = new PpuConfig(
                TilemapWidth: 2, TilemapHeight: 2,
                ColorMapCount: 0, BitsPerPixel: 1,
                TileSize: 8, ChrCount: 4, ChrInRom: true,
                SpriteCount: 3, BytesPerSprite: 3, MaxSpritesPerScanline: 2,
                CyclesPerScanline: 1, VBlankScanlineCount: 2, PpuCyclesPerCpuCycle: 1);
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // All 3 sprites at Y=0, covering scanlines [0, 8)
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x00, tileIndex: 0, attributes: 0);
            PpuTestHelpers.WriteOamEntry(vram, config, 1, position: 0x00, tileIndex: 1, attributes: 0);
            PpuTestHelpers.WriteOamEntry(vram, config, 2, position: 0x00, tileIndex: 2, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            evaluator.Evaluate(0, regs);
            Assert.That(regs.SpriteOverflow, Is.True);
        }

        [Test]
        public void Evaluate_OverflowDetected_StopsAtMax()
        {
            var config = new PpuConfig(
                TilemapWidth: 2, TilemapHeight: 2,
                ColorMapCount: 0, BitsPerPixel: 1,
                TileSize: 8, ChrCount: 4, ChrInRom: true,
                SpriteCount: 3, BytesPerSprite: 3, MaxSpritesPerScanline: 2,
                CyclesPerScanline: 1, VBlankScanlineCount: 2, PpuCyclesPerCpuCycle: 1);
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x00, tileIndex: 0, attributes: 0);
            PpuTestHelpers.WriteOamEntry(vram, config, 1, position: 0x00, tileIndex: 1, attributes: 0);
            PpuTestHelpers.WriteOamEntry(vram, config, 2, position: 0x00, tileIndex: 2, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(0, regs);
            Assert.That(sprites, Has.Length.EqualTo(2));
        }

        [Test]
        public void Evaluate_NoOverflow_ClearsSpriteOverflow()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            regs.SpriteOverflow = true;
            // No sprites written, so no overflow
            var evaluator = new SpriteEvaluator(vram, config);
            evaluator.Evaluate(0, regs);
            Assert.That(regs.SpriteOverflow, Is.False);
        }

        [Test]
        public void Evaluate_PreservesOamOrder()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var regs = new PpuRegisters(vram);
            // Both at Y=0 covering scanlines [0, 8)
            PpuTestHelpers.WriteOamEntry(vram, config, 0, position: 0x00, tileIndex: 0xAA, attributes: 0);
            PpuTestHelpers.WriteOamEntry(vram, config, 1, position: 0x00, tileIndex: 0xBB, attributes: 0);
            var evaluator = new SpriteEvaluator(vram, config);
            var sprites = evaluator.Evaluate(0, regs);
            Assert.Multiple(() =>
            {
                Assert.That(sprites[0].TileIndex, Is.EqualTo(0xAA));
                Assert.That(sprites[1].TileIndex, Is.EqualTo(0xBB));
            });
        }
    }
}
