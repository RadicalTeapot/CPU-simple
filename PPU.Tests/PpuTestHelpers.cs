using PPU.Configuration;
using PPU.Storage;

namespace PPU.Tests
{
    internal static class PpuTestHelpers
    {
        public static PpuConfig CreateMinimalConfig() => PpuConfig.Minimal8Bit;

        public static PpuConfig CreateSmallConfig() => new(
            TilemapWidth: 2, TilemapHeight: 2,
            ColorMapCount: 0, BitsPerPixel: 1,
            TileSize: 8, ChrCount: 4, ChrInRom: true,
            SpriteCount: 2, BytesPerSprite: 3, MaxSpritesPerScanline: 2,
            CyclesPerScanline: 1, VBlankScanlineCount: 2, PpuCyclesPerCpuCycle: 1);

        public static PpuConfig CreateTickConfig() => new(
            TilemapWidth: 2, TilemapHeight: 1,
            ColorMapCount: 0, BitsPerPixel: 1,
            TileSize: 8, ChrCount: 4, ChrInRom: true,
            SpriteCount: 2, BytesPerSprite: 3, MaxSpritesPerScanline: 2,
            CyclesPerScanline: 1, VBlankScanlineCount: 2, PpuCyclesPerCpuCycle: 1);

        public static PpuConfig CreateLargeVramConfig() => new(
            TilemapWidth: 16, TilemapHeight: 13,
            ColorMapCount: 0, BitsPerPixel: 1,
            TileSize: 8, ChrCount: 256, ChrInRom: false,
            SpriteCount: 16, BytesPerSprite: 3, MaxSpritesPerScanline: 8,
            CyclesPerScanline: 90, VBlankScanlineCount: 24, PpuCyclesPerCpuCycle: 3);

        public static ChrRom CreateChrRom(PpuConfig config, byte[]? tileData = null)
        {
            var size = config.BytesPerTile * config.ChrCount;
            var data = tileData ?? new byte[size];
            return new ChrRom(config, data);
        }

        public static ChrRom CreateChrRom(PpuConfig config, string chrromPath)
        {
            var size = config.BytesPerTile * config.ChrCount;
            var data = File.ReadAllBytes(chrromPath);
            if (data.Length != size)
                throw new ArgumentException($"CHR ROM file size does not match expected size: {size} bytes.");
            return new ChrRom(config, data);
        }

        public static byte[] CreateChrData(PpuConfig config, int tileIndex, byte[] rowBytes)
        {
            var data = new byte[config.BytesPerTile * config.ChrCount];
            var offset = tileIndex * config.BytesPerTile;
            for (int i = 0; i < rowBytes.Length && i < config.BytesPerTile; i++)
                data[offset + i] = rowBytes[i];
            return data;
        }

        public static ChrRom CreateChrRomWithTile(PpuConfig config, int tileIndex, byte[] rowBytes)
            => new ChrRom(config, CreateChrData(config, tileIndex, rowBytes));

        public static void WriteOamEntry(Vram vram, PpuConfig config, int spriteIndex,
            byte position, byte tileIndex, byte attributes)
        {
            var address = config.VramLayout.GetOamAddress(spriteIndex);
            vram.Write(address, position);
            vram.Write(address + 1, tileIndex);
            vram.Write(address + 2, attributes);
        }
    }
}
