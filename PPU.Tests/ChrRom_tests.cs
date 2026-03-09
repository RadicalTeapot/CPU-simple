using PPU.Configuration;
using PPU.Storage;

namespace PPU.Tests
{
    [TestFixture]
    public class ChrRom_tests
    {
        [Test]
        public void ReadTileRow_FirstTileFirstRow_ReturnsCorrectByte()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var rowBytes = new byte[] { 0xAA, 0, 0, 0, 0, 0, 0, 0 };
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, tileIndex: 0, rowBytes);
            Assert.That(rom.ReadTileRow(0, 0, vFlip: false), Is.EqualTo(0xAA));
        }

        [Test]
        public void ReadTileRow_SecondTile_ReadsFromCorrectOffset()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var rowBytes = new byte[] { 0xBB, 0, 0, 0, 0, 0, 0, 0 };
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, tileIndex: 1, rowBytes);
            Assert.That(rom.ReadTileRow(1, 0, vFlip: false), Is.EqualTo(0xBB));
        }

        [Test]
        public void ReadTileRow_VFlipFalse_ReadsNormalRow()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var rowBytes = new byte[] { 0x00, 0x00, 0xCC, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, tileIndex: 0, rowBytes);
            Assert.That(rom.ReadTileRow(0, 2, vFlip: false), Is.EqualTo(0xCC));
        }

        [Test]
        public void ReadTileRow_VFlipTrue_ReadsInvertedRow()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            // row 2 with vFlip reads row 7-2=5
            var rowBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0xDD, 0x00, 0x00 };
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, tileIndex: 0, rowBytes);
            Assert.That(rom.ReadTileRow(0, 2, vFlip: true), Is.EqualTo(0xDD));
        }

        [Test]
        public void ReadTileRow_VFlipTrue_Row0_ReadsRow7()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var rowBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xEE };
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, tileIndex: 0, rowBytes);
            Assert.That(rom.ReadTileRow(0, 0, vFlip: true), Is.EqualTo(0xEE));
        }

        [Test]
        public void ReadTileRow_VFlipTrue_Row7_ReadsRow0()
        {
            var config = PpuTestHelpers.CreateSmallConfig();
            var rowBytes = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var rom = PpuTestHelpers.CreateChrRomWithTile(config, tileIndex: 0, rowBytes);
            Assert.That(rom.ReadTileRow(0, 7, vFlip: true), Is.EqualTo(0xFF));
        }
    }
}
