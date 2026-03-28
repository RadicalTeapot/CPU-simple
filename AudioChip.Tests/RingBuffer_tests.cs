using AudioChip.Storage;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class RingBuffer_tests
    {
        [Test]
        public void AvailableSamples_WhenNew_IsZero()
        {
            var buffer = new RingBuffer(4);

            Assert.That(buffer.AvailableSamples, Is.EqualTo(0));
        }

        [Test]
        public void FreeSpace_WhenNew_EqualsSize()
        {
            var buffer = new RingBuffer(4);

            Assert.That(buffer.FreeSpace, Is.EqualTo(4));
        }

        [Test]
        public void Write_IncreasesAvailableSamples()
        {
            var buffer = new RingBuffer(4);

            buffer.Write(1.0f);

            Assert.That(buffer.AvailableSamples, Is.EqualTo(1));
        }

        [Test]
        public void Write_WhenFull_SilentlyDropsSample()
        {
            var buffer = new RingBuffer(2);
            buffer.Write(1.0f);
            buffer.Write(2.0f);

            buffer.Write(3.0f); // beyond capacity — should be dropped

            Assert.That(buffer.AvailableSamples, Is.EqualTo(2));
        }

        [Test]
        public void Read_WhenEmpty_ReturnsZero()
        {
            var buffer = new RingBuffer(4);

            Assert.That(buffer.Read(), Is.EqualTo(0.0f));
        }

        [Test]
        public void Read_AfterWrite_ReturnsSample()
        {
            var buffer = new RingBuffer(4);
            buffer.Write(0.5f);

            Assert.That(buffer.Read(), Is.EqualTo(0.5f));
        }

        [Test]
        public void Read_DecreasesAvailableSamples()
        {
            var buffer = new RingBuffer(4);
            buffer.Write(1.0f);
            buffer.Write(2.0f);

            buffer.Read();

            Assert.That(buffer.AvailableSamples, Is.EqualTo(1));
        }

        [Test]
        public void Read_AfterRead_RestoresFreeSpace()
        {
            var buffer = new RingBuffer(2);
            buffer.Write(1.0f);
            buffer.Write(2.0f);

            buffer.Read();

            Assert.That(buffer.FreeSpace, Is.EqualTo(1));
        }

        [Test]
        public void ReadWrite_MaintainsFifoOrder()
        {
            var buffer = new RingBuffer(4);
            buffer.Write(0.1f);
            buffer.Write(0.2f);
            buffer.Write(0.3f);

            Assert.Multiple(() =>
            {
                Assert.That(buffer.Read(), Is.EqualTo(0.1f).Within(1e-6f));
                Assert.That(buffer.Read(), Is.EqualTo(0.2f).Within(1e-6f));
                Assert.That(buffer.Read(), Is.EqualTo(0.3f).Within(1e-6f));
            });
        }

        [Test]
        public void WriteAndRead_WrapAroundBuffer_MaintainsCorrectOrder()
        {
            // Fill a 3-slot buffer, read one slot, then write a new sample that wraps the write pointer
            var buffer = new RingBuffer(3);
            buffer.Write(0.1f);
            buffer.Write(0.2f);
            buffer.Write(0.3f);
            buffer.Read(); // frees slot at position 0

            buffer.Write(0.4f); // wraps write pointer to position 0

            Assert.Multiple(() =>
            {
                Assert.That(buffer.Read(), Is.EqualTo(0.2f).Within(1e-6f));
                Assert.That(buffer.Read(), Is.EqualTo(0.3f).Within(1e-6f));
                Assert.That(buffer.Read(), Is.EqualTo(0.4f).Within(1e-6f));
            });
        }
    }
}
