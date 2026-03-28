using AudioChip.Configuration;
using AudioChip.Storage;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class AudioChain_tests
    {
        private const int SampleRate = 100;
        private static AudioConfiguration Config => new(SampleRate: SampleRate, BufferSize: 10, CpuClockRate: 100);

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
