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
                
                foreach (var sprite in activeSprites)
                {
                    var spriteCol = col - sprite.X * config.TileSize;
                    if (spriteCol < 0 || spriteCol >= config.TileSize)
                        continue;
                    var spritePixel = GetPixel(sprite.TileIndex, scanLine, spriteCol, sprite.FlipHorizontal, sprite.FlipVertical);
                    pixel = ResolvePixel(pixel, spritePixel, sprite.Prioritize);
                    break; // Only the first sprite pixel is considered for each column
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

        /// <summary>
        /// Determines which pixel should be displayed based on background and sprite pixel values.
        /// </summary>
        /// <remarks>The method accounts for sprite transparency and
        /// prioritization, ensuring correct visual output.
        /// If the sprite has priority, its pixel is returned even if it's transparent. Otherwise, the background pixel is returned if the sprite pixel is transparent, or the sprite pixel is returned if it's opaque.
        /// </remarks>
        /// <param name="bgPixel">The value of the background pixel. Used if the sprite pixel is transparent or does not have priority.</param>
        /// <param name="spritePixel">The value of the sprite pixel. If opaque or prioritized, this pixel is displayed.</param>
        /// <param name="spritePrioritize">A value indicating whether the sprite pixel should take precedence over the background pixel, regardless of
        /// transparency.</param>
        /// <returns>The value of the pixel to be displayed, taking into account background and sprite pixels as well as sprite prioritization.</returns>
        private static bool ResolvePixel(bool bgPixel, bool spritePixel, bool spritePrioritize)
            => spritePrioritize ? spritePixel : (spritePixel || bgPixel);
    }
}
