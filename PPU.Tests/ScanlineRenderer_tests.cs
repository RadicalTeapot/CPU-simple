using PPU.Configuration;
using PPU.Rendering;
using PPU.Storage;

namespace PPU.Tests
{
    [TestFixture]
    public class ScanlineRenderer_tests
    {
        [Test]
        public void RenderScanline_EmptyTilemap_AllPixelsZero()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var rom = PpuTestHelpers.CreateChrRom(config); // all zeros
            var renderer = new ScanlineRenderer(config, vram, rom);
            var fb = new Framebuffer(config);

            renderer.RenderScanline(0, [], fb);

            var pixels = fb.AsReadOnly();
            for (int i = 0; i < config.ScreenWidth; i++)
                Assert.That(pixels[i], Is.EqualTo(0), $"pixel at col {i}");
        }

        [Test]
        public void RenderScanline_SolidTile_AllPixelsOne()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            // Tile 1 = all 0xFF rows
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, 1,
                [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
            // Set tilemap: first tile column points to tile 1
            var layout = config.VramLayout;
            vram.Write(layout.GetTileAddress(0, 0), 1);
            var renderer = new ScanlineRenderer(config, vram, rom);
            var fb = new Framebuffer(config);

            renderer.RenderScanline(0, [], fb);

            var pixels = fb.AsReadOnly();
            // First 8 pixels (tile column 0) should be 1
            for (int i = 0; i < 8; i++)
                Assert.That(pixels[i], Is.EqualTo(1), $"pixel at col {i}");
        }

        [Test]
        public void RenderScanline_NoSprites_OnlyBackground()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, 1,
                [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
            vram.Write(config.VramLayout.GetTileAddress(0, 0), 1);
            var renderer = new ScanlineRenderer(config, vram, rom);
            var fb = new Framebuffer(config);

            renderer.RenderScanline(0, [], fb);

            // No sprites, so result is purely background
            Assert.That(fb.AsReadOnly()[0], Is.EqualTo(1));
        }

        [Test]
        public void RenderScanline_SpriteOverTransparentBg_SpriteWins()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            // Tile 0 = all zeros (transparent bg), Tile 2 = all 0xFF (opaque sprite)
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, 2,
                [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
            // BG tile 0 at position (0,0) → tile index 0 (transparent)
            var renderer = new ScanlineRenderer(config, vram, rom);
            var fb = new Framebuffer(config);
            // Sprite at X=0, Y=0, tileIndex=2, no priority
            var sprite = OamEntry.Decode(position: 0x00, tileIndex: 2, attributes: 0);

            renderer.RenderScanline(0, [sprite], fb);

            Assert.That(fb.AsReadOnly()[0], Is.EqualTo(1));
        }

        [Test]
        public void RenderScanline_SpritePrioritize_SpriteWins()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            // Tile 0 = all zeros (transparent bg), Tile 2 = all 0xFF (opaque sprite)
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, 2,
                [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
            var renderer = new ScanlineRenderer(config, vram, rom);
            var fb = new Framebuffer(config);
            // Sprite with prioritize=true (attribute bit 2)
            var sprite = OamEntry.Decode(position: 0x00, tileIndex: 2, attributes: 0b100);

            renderer.RenderScanline(0, [sprite], fb);

            Assert.That(fb.AsReadOnly()[0], Is.EqualTo(1));
        }

        [Test]
        public void RenderScanline_NoPrioritize_BothOpaque_ShowsOne()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            // Both bg tile and sprite tile are opaque (0xFF)
            var data = new byte[config.BytesPerTile * config.ChrCount];
            // Tile 1 = bg tile (all 0xFF)
            for (int i = 0; i < 8; i++) data[1 * 8 + i] = 0xFF;
            // Tile 2 = sprite tile (all 0xFF)
            for (int i = 0; i < 8; i++) data[2 * 8 + i] = 0xFF;
            var rom = new ChrRom(config, data);
            vram.Write(config.VramLayout.GetTileAddress(0, 0), 1); // bg = tile 1
            var renderer = new ScanlineRenderer(config, vram, rom);
            var fb = new Framebuffer(config);
            var sprite = OamEntry.Decode(position: 0x00, tileIndex: 2, attributes: 0);

            renderer.RenderScanline(0, [sprite], fb);

            // ResolvePixel with no prioritize: spritePixel || bgPixel = true
            Assert.That(fb.AsReadOnly()[0], Is.EqualTo(1));
        }

        [Test]
        public void RenderScanline_FirstSpriteWins()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var vram = new Vram(config);
            // Tile 2 = all 0xFF, Tile 3 = all 0xFF
            var data = new byte[config.BytesPerTile * config.ChrCount];
            for (int i = 0; i < 8; i++) data[2 * 8 + i] = 0xFF;
            for (int i = 0; i < 8; i++) data[3 * 8 + i] = 0xFF;
            var rom = new ChrRom(config, data);
            var renderer = new ScanlineRenderer(config, vram, rom);
            var fb = new Framebuffer(config);
            // Two sprites at same position, first should win (due to break in loop)
            var sprite1 = OamEntry.Decode(position: 0x00, tileIndex: 2, attributes: 0b100); // prioritize
            var sprite2 = OamEntry.Decode(position: 0x00, tileIndex: 3, attributes: 0);

            renderer.RenderScanline(0, [sprite1, sprite2], fb);

            // With prioritize on sprite1: pixel = spritePixel (true) → 1
            Assert.That(fb.AsReadOnly()[0], Is.EqualTo(1));
        }
    }
}
