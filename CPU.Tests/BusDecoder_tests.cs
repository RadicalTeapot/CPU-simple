using NUnit.Framework;
using CPU.components;
using CPU.microcode;

namespace CPU.Tests
{
    [TestFixture]
    public class BusDecoder_tests
    {
#if !x16
        [Test]
        public void ReadByte_RamAddress_ReadsFromMemory()
        {
            var memory = new Memory(232);
            memory.WriteByte(0x50, 0xAB);
            var bus = new BusDecoder(memory, new FakeMmioDevice());

            Assert.That(bus.ReadByte(0x50), Is.EqualTo(0xAB));
        }

        [Test]
        public void WriteByte_RamAddress_WritesToMemory()
        {
            var memory = new Memory(232);
            var bus = new BusDecoder(memory, new FakeMmioDevice());

            bus.WriteByte(0x50, 0xCD);

            Assert.That(memory.ReadByte(0x50), Is.EqualTo(0xCD));
        }

        [Test]
        public void ReadByte_MmioAddress_ForwardsToDevice()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0x42);
            var bus = new BusDecoder(new Memory(232), device);

            Assert.That(bus.ReadByte(0xE8), Is.EqualTo(0x42));
            Assert.That(device.LastReadOffset, Is.EqualTo(0));
        }

        [Test]
        public void ReadByte_MmioEndAddress_ForwardsToDevice()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0x77);
            var bus = new BusDecoder(new Memory(232), device);

            Assert.That(bus.ReadByte(0xEF), Is.EqualTo(0x77));
            Assert.That(device.LastReadOffset, Is.EqualTo(7));
        }

        [Test]
        public void WriteByte_MmioAddress_ForwardsToDevice()
        {
            var device = new FakeMmioDevice();
            var bus = new BusDecoder(new Memory(232), device);

            bus.WriteByte(0xEA, 0xFF);

            Assert.That(device.LastWriteOffset, Is.EqualTo(2));
            Assert.That(device.LastWriteValue, Is.EqualTo(0xFF));
        }

        [Test]
        public void ReadByte_ReservedAddress_ReturnsZero()
        {
            var bus = new BusDecoder(new Memory(232), new FakeMmioDevice());

            Assert.That(bus.ReadByte(0xF0), Is.EqualTo(0));
            Assert.That(bus.ReadByte(0xFF), Is.EqualTo(0));
        }

        [Test]
        public void WriteByte_ReservedAddress_SilentlyIgnored()
        {
            var device = new FakeMmioDevice();
            var bus = new BusDecoder(new Memory(232), device);

            bus.WriteByte(0xF0, 0xAB);

            Assert.That(device.LastWriteOffset, Is.Null);
        }

        [Test]
        public void Boundary_0xE7_GoesToMemory()
        {
            var memory = new Memory(232);
            memory.WriteByte(0xE7, 0x99);
            var bus = new BusDecoder(memory, new FakeMmioDevice());

            Assert.That(bus.ReadByte(0xE7), Is.EqualTo(0x99));
        }

        [Test]
        public void Boundary_0xE8_GoesToMmio()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0x11);
            var bus = new BusDecoder(new Memory(232), device);

            Assert.That(bus.ReadByte(0xE8), Is.EqualTo(0x11));
            Assert.That(device.LastReadOffset, Is.EqualTo(0));
        }

        [Test]
        public void Recorder_CalledForRamAccess()
        {
            var bus = new BusDecoder(new Memory(232), new FakeMmioDevice());
            var recorder = new BusRecorder();
            bus.Recorder = recorder;

            bus.ReadByte(0x10);
            Assert.That(recorder.LastAccess, Is.Not.Null);
            Assert.That(recorder.LastAccess!.Address, Is.EqualTo(0x10));
        }

        [Test]
        public void Recorder_CalledForMmioAccess()
        {
            var bus = new BusDecoder(new Memory(232), new FakeMmioDevice());
            var recorder = new BusRecorder();
            bus.Recorder = recorder;

            bus.WriteByte(0xE8, 0x42);
            Assert.That(recorder.LastAccess, Is.Not.Null);
            Assert.That(recorder.LastAccess!.Address, Is.EqualTo(0xE8));
        }

        [Test]
        public void Recorder_CalledForReservedAccess()
        {
            var bus = new BusDecoder(new Memory(232), new FakeMmioDevice());
            var recorder = new BusRecorder();
            bus.Recorder = recorder;

            bus.ReadByte(0xF5);
            Assert.That(recorder.LastAccess, Is.Not.Null);
            Assert.That(recorder.LastAccess!.Address, Is.EqualTo(0xF5));
        }
#else
        [Test]
        public void ReadByte_RamAddress_ReadsFromMemory()
        {
            var memory = new Memory(61184);
            memory.WriteByte(0x0050, 0xAB);
            var bus = new BusDecoder(memory, new FakeMmioDevice());

            Assert.That(bus.ReadByte(0x0050), Is.EqualTo(0xAB));
        }

        [Test]
        public void ReadByte_MmioAddress_ForwardsToDevice()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0x42);
            var bus = new BusDecoder(new Memory(61184), device);

            Assert.That(bus.ReadByte(0xEF00), Is.EqualTo(0x42));
            Assert.That(device.LastReadOffset, Is.EqualTo(0));
        }

        [Test]
        public void ReadByte_MmioEndAddress_ForwardsToDevice()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0x77);
            var bus = new BusDecoder(new Memory(61184), device);

            Assert.That(bus.ReadByte(0xEFFF), Is.EqualTo(0x77));
            Assert.That(device.LastReadOffset, Is.EqualTo(0xFF));
        }

        [Test]
        public void ReadByte_ReservedAddress_ReturnsZero()
        {
            var bus = new BusDecoder(new Memory(61184), new FakeMmioDevice());

            Assert.That(bus.ReadByte(0xFF00), Is.EqualTo(0));
            Assert.That(bus.ReadByte(0xFFFF), Is.EqualTo(0));
        }

        [Test]
        public void Boundary_0xEEFF_GoesToMemory()
        {
            var memory = new Memory(61184);
            memory.WriteByte(0xEEFF, 0x99);
            var bus = new BusDecoder(memory, new FakeMmioDevice());

            Assert.That(bus.ReadByte(0xEEFF), Is.EqualTo(0x99));
        }

        [Test]
        public void Boundary_0xEF00_GoesToMmio()
        {
            var device = new FakeMmioDevice();
            device.SetReadValue(0x11);
            var bus = new BusDecoder(new Memory(61184), device);

            Assert.That(bus.ReadByte(0xEF00), Is.EqualTo(0x11));
            Assert.That(device.LastReadOffset, Is.EqualTo(0));
        }
#endif

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
