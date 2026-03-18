using PPU.Configuration;
using PPU.Storage;

namespace PPU.Rendering
{
    /// <summary>
    /// Core rendering engine.
    /// Renders a single pixel scanline.
    /// </summary>
    internal class ScanlineRenderer(PpuConfig config, Vram vram, ChrRom rom)
    {
        public void RenderScanline(
            int scanLine, OamEntry[] activeSprites,
            Framebuffer framebuffer)
        {
            for (int col = 0; col < config.ScreenWidth; col++)
            {
                var pixel = GetBgPixel(scanLine, col);
                var hasPrioritized = false;

                foreach (var sprite in activeSprites)
                {
                    var spriteCol = col - sprite.X * config.TileSize;
                    if (spriteCol < 0 || spriteCol >= config.TileSize)
                        continue;
                    var spritePixel = GetPixel(sprite.TileIndex, scanLine, spriteCol, sprite.FlipHorizontal, sprite.FlipVertical);

                    if (sprite.Prioritize)
                    {
                        pixel = hasPrioritized ? pixel || spritePixel : spritePixel;
                        hasPrioritized = true;
                        if (pixel) break; // Once a prioritized sprite is opaque, remaining sprites cannot change the result.
                    }
                    else if (!hasPrioritized)
                    {
                        pixel = pixel || spritePixel;
                    }
                }

                framebuffer.Write(scanLine, col, (byte)(pixel ? 1 : 0));
            }
        }

        private bool GetBgPixel(int scanline, int pixelX)
        {
            var row = scanline / config.TileSize;
            var col = pixelX / config.TileSize;
            var tileAddress = config.VramLayout.GetTileAddress(col, row);
            var tileIdx = vram.Read(tileAddress);
            return GetPixel(tileIdx, scanline, pixelX, false, false); // BG doesn't support flipping
        }

        private bool GetPixel(byte tileIndex, int scanline, int pixelX, bool hFlip, bool vFlip)
        {
            var rowData = rom.ReadTileRow(tileIndex, scanline % config.TileSize, vFlip);
            return TileRow.GetPixel(rowData, pixelX % config.TileSize, hFlip);
        }

    }
}
