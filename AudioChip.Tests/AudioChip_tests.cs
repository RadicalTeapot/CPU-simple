using AudioChip.Configuration;
using AudioChip.Exceptions;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class AudioChip_tests
    {
        // SampleRate=100, CpuClockRate=100 → 1 sample per tick
        private static AudioConfiguration StandardConfig => new(SampleRate: 100, BufferSize: 10, CpuClockRate: 100);

        [Test]
        public void Tick_IncreasesSamplesAvailable()
        {
            var chip = new AudioChip(StandardConfig);
            var before = chip.AvailableSamples;

            chip.Tick();

            Assert.That(chip.AvailableSamples, Is.GreaterThan(before));
        }

        [Test]
        public void ConsumeSamples_ReturnsRequestedCount()
        {
            var chip = new AudioChip(StandardConfig);
            chip.Tick();
            chip.Tick();
            chip.Tick();

            var samples = chip.ConsumeSamples(2);

            Assert.That(samples.Length, Is.EqualTo(2));
        }

        [Test]
        public void ConsumeSamples_ReducesSamplesAvailable()
        {
            var chip = new AudioChip(StandardConfig);
            chip.Tick();
            chip.Tick();
            chip.Tick();
            var before = chip.AvailableSamples;

            chip.ConsumeSamples(2);

            Assert.That(chip.AvailableSamples, Is.EqualTo(before - 2));
        }

        [Test]
        public void ConsumeSamples_WhenMoreThanAvailable_ThrowsBufferUnderrunException()
        {
            var chip = new AudioChip(StandardConfig);
            chip.Tick(); // 1 sample available

            Assert.That(() => chip.ConsumeSamples(2), Throws.InstanceOf<AudioChipExceptions.BufferUnderrunException>());
        }

        [Test]
        public void Tick_WhenBufferFull_ThrowsBufferOverrunException()
        {
            // samplesPerTick=8, bufferSize=2 → buffer capacity=8; first Tick fills it
            var config = new AudioConfiguration(SampleRate: 8, BufferSize: 2, CpuClockRate: 1);
            var chip = new AudioChip(config);
            chip.Tick();

            Assert.That(() => chip.Tick(), Throws.InstanceOf<AudioChipExceptions.BufferOverrunException>());
        }
    }
}
