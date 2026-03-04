using System.Runtime.CompilerServices;

namespace PPU.Configuration
{
    internal class PpuVramLayout
    {
        public int TotalSize { get; }

        public PpuVramLayout(int tileSize, int chrCount,
            int tilemapWidth, int tilemapHeight,
            int spriteCount, int bytesPerSprite)
        {
            _chrTableBase = 0;
            _chrTableSize = tileSize * chrCount;
            _tilemapBase = _chrTableBase + _chrTableSize;
            _tilemapSize = tilemapWidth * tilemapHeight;
            _oamBase = _tilemapBase + _tilemapSize;
            _tilemapWidth = tilemapWidth;
            _bytesPerSprite = bytesPerSprite;
            TotalSize = _chrTableSize + _tilemapSize + spriteCount;
        }

        public int GetTileAddress(int col, int row) => _tilemapBase + (row * _tilemapWidth) + col;
        public int GetOamAddress(int spriteIndex) => _oamBase + spriteIndex * _bytesPerSprite;

        private readonly int _tilemapBase;
        private readonly int _oamBase;
        private readonly int _chrTableBase;
        private readonly int _chrTableSize;
        private readonly int _tilemapSize;
        private readonly int _tilemapWidth;
        private readonly int _bytesPerSprite;
    }
}
