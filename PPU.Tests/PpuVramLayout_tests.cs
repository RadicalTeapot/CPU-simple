using PPU.Configuration;

namespace PPU.Tests
{
    [TestFixture]
    public class PpuVramLayout_tests
    {
        [Test]
        public void ColormapBase_NoChr_IsZero()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            Assert.That(layout.ColormapBase, Is.EqualTo(0));
        }

        [Test]
        public void ColormapBase_WithChr_FollowsChrTable()
        {
            var layout = new PpuVramLayout(chrCount: 4, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 2, tilemapHeight: 2,
                spriteCount: 2, bytesPerSprite: 3);
            // 4 tiles * 8 bytes = 32
            Assert.That(layout.ColormapBase, Is.EqualTo(32));
        }

        [Test]
        public void TilemapBase_FollowsColormap()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 4, bitsPerPixel: 8,
                tilemapWidth: 2, tilemapHeight: 2,
                spriteCount: 2, bytesPerSprite: 3);
            // ColormapBase=0, ColormapSize=4*8/8=4, TilemapBase=4
            Assert.That(layout.TilemapBase, Is.EqualTo(4));
        }

        [Test]
        public void TilemapSize_IsWidthTimesHeight()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            Assert.That(layout.TilemapSize, Is.EqualTo(208));
        }

        [Test]
        public void OamBase_FollowsTilemap()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            // TilemapBase=0, TilemapSize=208
            Assert.That(layout.OamBase, Is.EqualTo(208));
        }

        [Test]
        public void OamSize_IsSpriteCountTimesBytesPerSprite()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            Assert.That(layout.OamSize, Is.EqualTo(48));
        }

        [Test]
        public void TotalSize_IncludesColormapSize()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 4, bitsPerPixel: 8,
                tilemapWidth: 2, tilemapHeight: 2,
                spriteCount: 2, bytesPerSprite: 3);
            // chrTable=0, colormap=4*8/8=4, tilemap=2*2=4, oam=2*3=6 → total=14
            Assert.That(layout.TotalSize, Is.EqualTo(14));
        }

        [Test]
        public void TotalSize_Minimal8Bit_Is256()
        {
            var config = PpuTestHelpers.CreateMinimalConfig();
            Assert.That(config.VramLayout.TotalSize, Is.EqualTo(256));
        }

        [Test]
        public void GetTileAddress_Origin_ReturnsTilemapBase()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            Assert.That(layout.GetTileAddress(0, 0), Is.EqualTo(layout.TilemapBase));
        }

        [Test]
        public void GetTileAddress_ColAndRow_CalculatesCorrectly()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            // col=3, row=2: base + 2*16 + 3 = base + 35
            Assert.That(layout.GetTileAddress(3, 2), Is.EqualTo(layout.TilemapBase + 35));
        }

        [Test]
        public void GetOamAddress_FirstSprite_ReturnsOamBase()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            Assert.That(layout.GetOamAddress(0), Is.EqualTo(layout.OamBase));
        }

        [Test]
        public void GetOamAddress_ThirdSprite_ReturnsCorrectOffset()
        {
            var layout = new PpuVramLayout(chrCount: 0, bytesPerTile: 8,
                colormapCount: 0, bitsPerPixel: 1,
                tilemapWidth: 16, tilemapHeight: 13,
                spriteCount: 16, bytesPerSprite: 3);
            // index=2: OamBase + 2*3 = OamBase + 6
            Assert.That(layout.GetOamAddress(2), Is.EqualTo(layout.OamBase + 6));
        }
    }
}
