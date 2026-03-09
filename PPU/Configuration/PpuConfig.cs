namespace PPU.Configuration
{
    public record PpuConfig(
        int TilemapWidth, int TilemapHeight,
        int ColorMapCount, int BitsPerPixel,
        int TileSize, int ChrCount, bool ChrInRom,
        int SpriteCount, int BytesPerSprite, int MaxSpritesPerScanline,
        int CyclesPerScanline, int VBlankScanlineCount,
        int PpuCyclesPerCpuCycle)
    {
        // No memoization for those 3 properties needed as this is a simple calculation.
        public int BytesPerTile => (TileSize * TileSize * BitsPerPixel) / 8;
        public int ScreenWidth => TilemapWidth * TileSize;
        public int ScreenHeight => TilemapHeight * TileSize;
        public int TotalScanlines => ScreenHeight + VBlankScanlineCount;

        internal PpuVramLayout VramLayout => 
            _vramLayout ??= new(
                ChrInRom ? 0 : ChrCount, BytesPerTile, 
                ColorMapCount, BitsPerPixel, 
                TilemapWidth, TilemapHeight, 
                SpriteCount, BytesPerSprite);

        public static PpuConfig Minimal8Bit => new(
            TilemapWidth: 16, TilemapHeight: 13,
            ColorMapCount: 0, BitsPerPixel: 1,
            TileSize: 8, ChrCount: 256, ChrInRom: true,
            SpriteCount: 16, BytesPerSprite: 3, MaxSpritesPerScanline: 8,
            CyclesPerScanline: 90, VBlankScanlineCount: 24, PpuCyclesPerCpuCycle: 3); // Play with those values to find the sweet spot between performance and visual quality.

        private PpuVramLayout? _vramLayout; // Backing field for memoization of the VramLayout.
    }
}
