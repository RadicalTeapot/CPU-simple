using AudioChip.Configuration;
using AudioChip.Storage;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class AudioChain_tests
    {
        private const int SampleRate = 100;
        private static AudioConfiguration Config => new(SampleRate: SampleRate, BufferSize: 10, CpuClockRate: 100);

        private const int FilterSampleRate = 8000;
        private static AudioConfiguration FilterConfig => new(SampleRate: FilterSampleRate, BufferSize: 10, CpuClockRate: 100);

        [Test]
        public void GetSample_WithGateOff_ReturnsZero()
        {
            // Voice.Default has gate=false → oscillator envelope stays at min (0) → VCA outputs 0
            var voice = Voice.Default;
            var chain = new AudioChain(Config, voice);

            Assert.That(chain.GetSample(), Is.EqualTo(0.0f));
        }

        [Test]
        public void GetSample_WithGateOn_ReturnsNonZero()
        {
            var voice = Voice.Default;
            voice.Gate = true;
            var chain = new AudioChain(Config, voice);

            // Advance past the attack phase so the envelope has risen
            for (int i = 0; i < SampleRate; i++)
                chain.GetSample();

            Assert.That(chain.GetSample(), Is.Not.EqualTo(0.0f));
        }

        [Test]
        public void GetSample_FilterUseEnvelopeTrue_FilterStaysOpenAtStartOfRelease()
        {
            // UseEnvelope=true: filter borrows the note's release time → on the first gate-off sample
            // the filter envelope hasn't had time to ramp down, so the filter stays at full cutoff
            // and the oscillator signal passes through.
            var voice = Voice.Default;
            voice.Envelope = new Envelope(attack: 0f, sustain: 1.0f, release: 1.0f);
            voice.Filter = new Filter(1000, FilterType.LowPass, FilterSlope.Slope12dB, useEnvelope: true);
            var chain = new AudioChain(FilterConfig, voice);

            var sample = chain.GetSample();

            Assert.That(Math.Abs(sample), Is.GreaterThan(0.01f));
        }

        [Test]
        public void GetSample_FilterUseEnvelopeFalse_FilterClosesInstantlyOnGateOff()
        {
            // UseEnvelope=false (fixed mode): filter release is always 0 → cutoff snaps to 0 on gate-off,
            // clamped to 1 Hz by the biquad, so the oscillator signal is effectively blocked.
            var voice = Voice.Default;
            voice.Envelope = new Envelope(attack: 0f, sustain: 1.0f, release: 1.0f);
            voice.Filter = new Filter(1000, FilterType.LowPass, FilterSlope.Slope12dB, useEnvelope: false);
            var chain = new AudioChain(FilterConfig, voice);

            var sample = chain.GetSample();

            Assert.That(Math.Abs(sample), Is.LessThan(1e-5f));
        }

        [Test]
        public void GetSample_AlwaysReturnsValueInRange()
        {
            var voice = Voice.Default;
            voice.Gate = true;
            var chain = new AudioChain(Config, voice);

            for (int i = 0; i < SampleRate * 2; i++)
            {
                var sample = chain.GetSample();
                Assert.That(sample, Is.InRange(-1.0f, 1.0f), $"Sample {i} out of range: {sample}");
            }
        }
    }
}
