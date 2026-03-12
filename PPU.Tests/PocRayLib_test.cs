using PPU.Configuration;
using PPU.Rendering;
using PPU.Storage;
using Raylib_cs;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace PPU.Tests
{
    [TestFixture]
    public class PocRaylib_test
    {
        [Test]
        [Ignore("POC window")]
        public void Raylib_Test()
        {
            InitWindow(640, 360, "Raylib-cs on Arch");
            SetTargetFPS(60);

            while (!WindowShouldClose())
            {
                BeginDrawing();
                ClearBackground(Color.Black);
                DrawText("Hello from raylib-cs", 20, 20, 20, Color.RayWhite);
                EndDrawing();
            }

            CloseWindow();
            Assert.Pass();
        }

        [Test]
        [Ignore("POC window")]
        public void Raylib_Framebuffer()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            var path = "../../../../data/ppu_chrrom/default.rom";
            var chrRom = PpuTestHelpers.CreateChrRom(config, path);

            var vram = new Vram(config);
            for (var row = 0; row < config.TilemapHeight; row++)
            {
                for (var col = 0; col < config.TilemapWidth; col++)
                {
                    var border = row == 0 || row == config.TilemapHeight - 1
                               || col == 0 || col == config.TilemapWidth - 1;
                    vram.Write(config.VramLayout.GetTileAddress(col, row), (byte)(row * config.TilemapWidth + col));
                }
            }

            // Render all scanlines into framebuffer
            var scanlineRenderer = new ScanlineRenderer(config, vram, chrRom);
            var framebuffer = new Framebuffer(config);
            for (var scanline = 0; scanline < config.ScreenHeight; scanline++)
                scanlineRenderer.RenderScanline(scanline, [], framebuffer);

            // Map 1bpp pixel bytes to Raylib colors
            var pixels = framebuffer.AsReadOnly();
            var colors = new Color[pixels.Count];
            for (var i = 0; i < pixels.Count; i++)
                colors[i] = pixels[i] != 0 ? Color.RayWhite : Color.Black;

            const int scale = 4;
            InitWindow(config.ScreenWidth * scale, config.ScreenHeight * scale, "PPU Framebuffer");
            SetTargetFPS(60);

            var image = GenImageColor(config.ScreenWidth, config.ScreenHeight, Color.Black);
            var texture = LoadTextureFromImage(image);
            UnloadImage(image);
            UpdateTexture(texture, colors);

            while (!WindowShouldClose())
            {
                BeginDrawing();
                ClearBackground(Color.Black);
                DrawTextureEx(texture, Vector2.Zero, 0f, scale, Color.White);
                EndDrawing();
            }

            UnloadTexture(texture);
            CloseWindow();
            Assert.Pass();
        }
    }
}