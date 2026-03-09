using PPU.Configuration;
using PPU.Rendering;
using PPU.Storage;
using Raylib_cs;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace PPU.Tests
{
    [TestFixture]
    public class poc_raylib
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

            // Build tile data: tile 0 = blank, tile 1 = checkerboard, tile 2 = solid
            var tileData = new byte[config.BytesPerTile * config.ChrCount];
            int tile1 = 1 * config.BytesPerTile;
            int tile2 = 2 * config.BytesPerTile;
            for (int row = 0; row < config.TileSize; row++)
            {
                tileData[tile1 + row] = (byte)(row % 2 == 0 ? 0xAA : 0x55);
                tileData[tile2 + row] = 0xFF;
            }
            var chrRom = PpuTestHelpers.CreateChrRom(config, tileData);

            // Fill VRAM tilemap: solid border (tile 2), checkerboard interior (tile 1)
            var vram = new Vram(config);
            for (int row = 0; row < config.TilemapHeight; row++)
            {
                for (int col = 0; col < config.TilemapWidth; col++)
                {
                    bool border = row == 0 || row == config.TilemapHeight - 1
                               || col == 0 || col == config.TilemapWidth - 1;
                    vram.Write(config.VramLayout.GetTileAddress(col, row), (byte)(border ? 2 : 1));
                }
            }

            // Render all scanlines into framebuffer
            var scanlineRenderer = new ScanlineRenderer(config, vram, chrRom);
            var framebuffer = new Framebuffer(config);
            for (int scanline = 0; scanline < config.ScreenHeight; scanline++)
                scanlineRenderer.RenderScanline(scanline, [], framebuffer);

            // Map 1bpp pixel bytes to Raylib colors
            var pixels = framebuffer.AsReadOnly();
            var colors = new Color[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
                colors[i] = pixels[i] != 0 ? Color.Black : Color.RayWhite;

            const int scale = 8;
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