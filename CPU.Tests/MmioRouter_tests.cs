using NUnit.Framework;
using CPU.components;

namespace CPU.Tests
{
    [TestFixture]
    public class MmioRouter_tests
    {
        [Test]
        public void Register_SingleDevice_Succeeds()
        {
            var router = new MmioRouter();
            Assert.DoesNotThrow(() => router.Register(0, 4, new FakeMmioDevice()));
        }

        [Test]
        public void Register_MultipleNonOverlapping_Succeeds()
        {
            var router = new MmioRouter();
            router.Register(0, 2, new FakeMmioDevice());
            Assert.DoesNotThrow(() => router.Register(4, 2, new FakeMmioDevice()));
        }

        [Test]
        public void Register_AdjacentDevices_Succeeds()
        {
            var router = new MmioRouter();
            router.Register(0, 3, new FakeMmioDevice());
            Assert.DoesNotThrow(() => router.Register(3, 1, new FakeMmioDevice()));
        }

        [Test]
        public void Register_OverlappingDevices_ThrowsArgumentException()
        {
            var router = new MmioRouter();
            router.Register(0, 4, new FakeMmioDevice());
            Assert.Throws<ArgumentException>(() => router.Register(2, 4, new FakeMmioDevice()));
        }

        [Test]
        public void Register_ZeroSize_ThrowsArgumentException()
        {
            var router = new MmioRouter();
            Assert.Throws<ArgumentException>(() => router.Register(0, 0, new FakeMmioDevice()));
        }

        [Test]
        public void Register_Overflow_ThrowsArgumentException()
        {
            var router = new MmioRouter();
            Assert.Throws<ArgumentException>(() => router.Register(250, 10, new FakeMmioDevice()));
        }

        [Test]
        public void ReadRegister_MappedOffset_ForwardsToDevice()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0x42);
            var router = new MmioRouter();
            router.Register(4, 2, device);

            var result = router.ReadRegister(5);

            Assert.That(result, Is.EqualTo(0x42));
            Assert.That(device.LastReadOffset, Is.EqualTo(1));
        }

        [Test]
        public void ReadRegister_ExactBaseOffset_GivesRelativeOffsetZero()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0xAB);
            var router = new MmioRouter();
            router.Register(10, 4, device);

            var result = router.ReadRegister(10);

            Assert.That(result, Is.EqualTo(0xAB));
            Assert.That(device.LastReadOffset, Is.EqualTo(0));
        }

        [Test]
        public void ReadRegister_UnmappedOffset_ReturnsZero()
        {
            var router = new MmioRouter();
            router.Register(0, 2, new FakeMmioDevice());

            var result = router.ReadRegister(5);

            Assert.That(result, Is.EqualTo(0x00));
        }

        [Test]
        public void WriteRegister_MappedOffset_ForwardsToDevice()
        {
            var device = new FakeMmioDevice();
            var router = new MmioRouter();
            router.Register(4, 2, device);

            router.WriteRegister(5, 0xFF);

            Assert.That(device.LastWriteOffset, Is.EqualTo(1));
            Assert.That(device.LastWriteValue, Is.EqualTo(0xFF));
        }

        [Test]
        public void WriteRegister_UnmappedOffset_DoesNotThrow()
        {
            var router = new MmioRouter();
            Assert.DoesNotThrow(() => router.WriteRegister(10, 0xFF));
        }

        [Test]
        public void MultiDevice_RoutesToCorrectDevice()
        {
            var deviceA = new FakeMmioDevice();
            deviceA.SetReadValue(0xAA);
            var deviceB = new FakeMmioDevice();
            deviceB.SetReadValue(0xBB);

            var router = new MmioRouter();
            router.Register(0, 2, deviceA);
            router.Register(4, 2, deviceB);

            Assert.That(router.ReadRegister(0), Is.EqualTo(0xAA));
            Assert.That(router.ReadRegister(4), Is.EqualTo(0xBB));

            router.WriteRegister(1, 0x11);
            Assert.That(deviceA.LastWriteOffset, Is.EqualTo(1));
            Assert.That(deviceA.LastWriteValue, Is.EqualTo(0x11));

            router.WriteRegister(5, 0x22);
            Assert.That(deviceB.LastWriteOffset, Is.EqualTo(1));
            Assert.That(deviceB.LastWriteValue, Is.EqualTo(0x22));
        }

        private class FakeMmioDevice : IMmioDevice
        {
            public byte? LastReadOffset { get; private set; }
            public byte? LastWriteOffset { get; private set; }
            public byte? LastWriteValue { get; private set; }

            public void SetReadValue(byte value) => _readValue = value;

            public byte ReadRegister(byte offset)
            {
                LastReadOffset = offset;
                return _readValue;
            }

            public void WriteRegister(byte offset, byte value)
            {
                LastWriteOffset = offset;
                LastWriteValue = value;
            }

            private byte _readValue;
        }
    }
}
