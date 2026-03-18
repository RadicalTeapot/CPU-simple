using PPU.Storage;

namespace PPU.Tests
{
    [TestFixture]
    public class TileRow_tests
    {
        [TestCase(0, true)]
        [TestCase(3, true)]
        [TestCase(7, true)]
        public void GetPixel_AllOnes_ReturnsTrue(int col, bool expected)
        {
            Assert.That(TileRow.GetPixel(0xFF, col, hFlip: false), Is.EqualTo(expected));
        }

        [TestCase(0, false)]
        [TestCase(3, false)]
        [TestCase(7, false)]
        public void GetPixel_AllZeros_ReturnsFalse(int col, bool expected)
        {
            Assert.That(TileRow.GetPixel(0x00, col, hFlip: false), Is.EqualTo(expected));
        }

        [Test]
        public void GetPixel_MsbSet_Col0NoFlip_ReturnsTrue()
        {
            Assert.That(TileRow.GetPixel(0x80, 0, hFlip: false), Is.True);
        }

        [Test]
        public void GetPixel_MsbSet_Col7NoFlip_ReturnsFalse()
        {
            Assert.That(TileRow.GetPixel(0x80, 7, hFlip: false), Is.False);
        }

        [Test]
        public void GetPixel_LsbSet_Col7NoFlip_ReturnsTrue()
        {
            Assert.That(TileRow.GetPixel(0x01, 7, hFlip: false), Is.True);
        }

        [Test]
        public void GetPixel_HFlip_MirrorsBitIndex()
        {
            // 0x80 = bit 7 set. Without flip: col 0 = true. With flip: col 7 = true, col 0 = false
            Assert.That(TileRow.GetPixel(0x80, 7, hFlip: true), Is.True);
            Assert.That(TileRow.GetPixel(0x80, 0, hFlip: true), Is.False);
        }

        [Test]
        public void GetPixel_AlternatingBits_CorrectPattern()
        {
            // 0xAA = 10101010
            // Without flip: col 0=1, 1=0, 2=1, 3=0, 4=1, 5=0, 6=1, 7=0
            bool[] expectedNoFlip = [true, false, true, false, true, false, true, false];
            bool[] expectedFlip = [false, true, false, true, false, true, false, true];

            for (int col = 0; col < 8; col++)
            {
                Assert.That(TileRow.GetPixel(0xAA, col, hFlip: false), Is.EqualTo(expectedNoFlip[col]),
                    $"NoFlip col={col}");
                Assert.That(TileRow.GetPixel(0xAA, col, hFlip: true), Is.EqualTo(expectedFlip[col]),
                    $"Flip col={col}");
            }
        }
    }
}
