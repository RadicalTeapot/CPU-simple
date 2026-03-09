using System.Runtime.CompilerServices;

namespace PPU.Configuration
{
    internal class PpuVramLayout
    {
        public int TotalSize { get; }
        public int ColormapBase { get; }
        public int ColormapSize { get; }
        public int TilemapBase { get; }
        public int TilemapSize { get; }
        public int OamBase { get; }
        public int OamSize { get; }

        public PpuVramLayout(int chrCount, int bytesPerTile,
            int colormapCount, int bitsPerPixel,
            int tilemapWidth, int tilemapHeight,
            int spriteCount, int bytesPerSprite)
        {
            _tilemapWidth = tilemapWidth;
            _bytesPerSprite = bytesPerSprite;

            // CHR table starts at 0 (and is empty if CHR data is in ROM)
            var chrTableBase = 0;
            var chrTableSize = bytesPerTile * chrCount;
            // Colormap immediately follows the CHR table
            ColormapBase = chrTableBase + chrTableSize;
            ColormapSize = colormapCount * bitsPerPixel / 8;
            // Tilemap immediately follows the colormap
            TilemapBase = ColormapBase + ColormapSize;
            TilemapSize = tilemapWidth * tilemapHeight;
            // OAM immediately follows the tilemap
            OamBase = TilemapBase + TilemapSize;
            OamSize = spriteCount * bytesPerSprite;

            TotalSize = chrTableSize + TilemapSize + OamSize;
        }

        public int GetTileAddress(int col, int row) => TilemapBase + (row * _tilemapWidth) + col;
        public int GetOamAddress(int spriteIndex) => OamBase + spriteIndex * _bytesPerSprite;


        private readonly int _bytesPerSprite;
        private readonly int _tilemapWidth;
    }
}
