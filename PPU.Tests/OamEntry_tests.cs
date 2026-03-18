using PPU.Rendering;

namespace PPU.Tests
{
    [TestFixture]
    public class OamEntry_tests
    {
        [Test]
        public void Decode_Position_ExtractsXFromLowNibble()
        {
            var entry = OamEntry.Decode(position: 0x53, tileIndex: 0, attributes: 0);
            Assert.That(entry.X, Is.EqualTo(3));
        }

        [Test]
        public void Decode_Position_ExtractsYFromHighNibble()
        {
            var entry = OamEntry.Decode(position: 0x53, tileIndex: 0, attributes: 0);
            Assert.That(entry.Y, Is.EqualTo(5));
        }

        [Test]
        public void Decode_TileIndex_StoredDirectly()
        {
            var entry = OamEntry.Decode(position: 0x00, tileIndex: 0xAB, attributes: 0);
            Assert.That(entry.TileIndex, Is.EqualTo(0xAB));
        }

        [Test]
        public void Decode_Attributes_Bit2_SetsPrioritize()
        {
            var entry = OamEntry.Decode(position: 0x00, tileIndex: 0, attributes: 0b100);
            Assert.Multiple(() =>
            {
                Assert.That(entry.Prioritize, Is.True);
                Assert.That(entry.FlipHorizontal, Is.False);
                Assert.That(entry.FlipVertical, Is.False);
            });
        }

        [Test]
        public void Decode_Attributes_Bit1_SetsFlipHorizontal()
        {
            var entry = OamEntry.Decode(position: 0x00, tileIndex: 0, attributes: 0b010);
            Assert.Multiple(() =>
            {
                Assert.That(entry.Prioritize, Is.False);
                Assert.That(entry.FlipHorizontal, Is.True);
                Assert.That(entry.FlipVertical, Is.False);
            });
        }

        [Test]
        public void Decode_Attributes_Bit0_SetsFlipVertical()
        {
            var entry = OamEntry.Decode(position: 0x00, tileIndex: 0, attributes: 0b001);
            Assert.Multiple(() =>
            {
                Assert.That(entry.Prioritize, Is.False);
                Assert.That(entry.FlipHorizontal, Is.False);
                Assert.That(entry.FlipVertical, Is.True);
            });
        }

        [Test]
        public void Decode_Attributes_AllSet_AllFlagsTrue()
        {
            var entry = OamEntry.Decode(position: 0x00, tileIndex: 0, attributes: 0b111);
            Assert.Multiple(() =>
            {
                Assert.That(entry.Prioritize, Is.True);
                Assert.That(entry.FlipHorizontal, Is.True);
                Assert.That(entry.FlipVertical, Is.True);
            });
        }

        [Test]
        public void Decode_Attributes_NoneSet_AllFlagsFalse()
        {
            var entry = OamEntry.Decode(position: 0x00, tileIndex: 0, attributes: 0b000);
            Assert.Multiple(() =>
            {
                Assert.That(entry.Prioritize, Is.False);
                Assert.That(entry.FlipHorizontal, Is.False);
                Assert.That(entry.FlipVertical, Is.False);
            });
        }

        [Test]
        public void Decode_Position_ZeroZero()
        {
            var entry = OamEntry.Decode(position: 0x00, tileIndex: 0, attributes: 0);
            Assert.Multiple(() =>
            {
                Assert.That(entry.X, Is.EqualTo(0));
                Assert.That(entry.Y, Is.EqualTo(0));
            });
        }

        [Test]
        public void Decode_Position_MaxValues()
        {
            var entry = OamEntry.Decode(position: 0xFF, tileIndex: 0, attributes: 0);
            Assert.Multiple(() =>
            {
                Assert.That(entry.X, Is.EqualTo(15));
                Assert.That(entry.Y, Is.EqualTo(15));
            });
        }
    }
}
