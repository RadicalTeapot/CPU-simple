using PPU.Configuration;
using PPU.Storage;

namespace PPU.Rendering
{
    internal class SpriteEvaluator(Vram vram, PpuConfig config)
    {
        /// <summary>
        /// Evaluates the sprites that should be rendered on the current scanline based on their Y position and the current tile row.
        /// </summary>
        /// <param name="scanlineIndex">The index of the current scanline.</param>
        /// <param name="registers">The PPU registers.</param>
        /// <returns>An array of OamEntry representing the sprites to be rendered on the current scanline.</returns>
        /// <remarks>
        /// The method also sets the SpriteOverflow flag in the PPU registers if more than the maximum allowed sprites are found for the scanline.
        /// The order of sprites in OAM is preserved to ensure correct priority handling during rendering.
        /// </remarks>
        public OamEntry[] Evaluate(int scanlineIndex, PpuRegisters registers)
        {
            // Note: an optimization could be made when sprites are tile aligned to cache the result for each 8 row block
            var sprites = new List<OamEntry>();
            var layout = config.VramLayout;
            registers.SpriteOverflow = false;
            for (int i = 0; i < config.SpriteCount; i++) // Preserve the order of sprites in OAM to ensure correct priority handling
            {
                var address = layout.GetOamAddress(i);
                var oamEntry = OamEntry.Decode(
                    position: vram.Read(address), 
                    tileIndex: vram.Read(address + 1), 
                    attributes: vram.Read(address + 2));
                if (scanlineIndex >= oamEntry.Y && scanlineIndex < oamEntry.Y + config.TileSize)
                {
                    if (sprites.Count >= config.MaxSpritesPerScanline)
                    {
                        registers.SpriteOverflow = true;
                        break;
                    }
                    sprites.Add(oamEntry);
                }
            }
            return [..sprites];
        }
    }
}
