using AudioChip.Generators;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class PulseWaveOscillator_tests
    {
        [Test]
        public void NextSample_WhenPhaseIsZero_ReturnsPositiveOne()
        {
            var osc = new PulseWaveOscillator(sampleRate: 44100, phase: 0.0f);

            Assert.That(osc.NextSample(frequency: 440, dutyCycle: 0.5f), Is.EqualTo(1.0f));
        }

        [Test]
        public void NextSample_WhenPhaseExceedsDutyCycle_ReturnsNegativeOne()
        {
            var osc = new PulseWaveOscillator(sampleRate: 44100, phase: 0.75f);

            Assert.That(osc.NextSample(frequency: 440, dutyCycle: 0.5f), Is.EqualTo(-1.0f));
        }

        // NOTE: The following two tests currently fail because frequency/sampleRate uses integer
        // division (e.g. 1/16 = 0), so phase never advances for typical audio frequencies.
        // Fix: change _phase += frequency / sampleRate to _phase += (float)frequency / sampleRate;

        [TestCase(0.0625f, 1, 15)]
        [TestCase(0.125f,  2, 14)]
        [TestCase(0.25f,   4, 12)]
        [TestCase(0.5f,    8,  8)]
        public void NextSample_DutyCycleRatioOverOnePeriod_CorrectHighLowCount(float dutyCycle, int expectedHigh, int expectedLow)
        {
            // sampleRate=16, frequency=1 → period of 16 samples, phase advances 0.0625 per sample
            var osc = new PulseWaveOscillator(sampleRate: 16, phase: 0.0f);

            int high = 0, low = 0;
            for (int i = 0; i < 16; i++)
            {
                var s = osc.NextSample(frequency: 1, dutyCycle: dutyCycle);
                if (s > 0) high++; else low++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(high, Is.EqualTo(expectedHigh));
                Assert.That(low, Is.EqualTo(expectedLow));
            });
        }

        [Test]
        public void NextSample_PhaseWrapsAfterOnePeriod_FirstSampleOfNextPeriodIsPositiveOne()
        {
            // sampleRate=4, frequency=1 → period of 4 samples, phase advances 0.25 per sample
            var osc = new PulseWaveOscillator(sampleRate: 4, phase: 0.0f);

            for (int i = 0; i < 4; i++)
                osc.NextSample(frequency: 1, dutyCycle: 0.5f);

            // Phase should have wrapped back to ~0; first sample of next period should be +1
            Assert.That(osc.NextSample(frequency: 1, dutyCycle: 0.5f), Is.EqualTo(1.0f));
        }

        [Test]
        public void SetPhase_ReturnsNewPhase()
        {
            var osc = new PulseWaveOscillator(sampleRate: 44100, phase: 0.0f);

            Assert.That(osc.SetPhase(0.3f), Is.EqualTo(0.3f).Within(1e-6f));
        }

        [Test]
        public void SetPhase_WithValueGreaterThanOne_WrapsAroundModuloOne()
        {
            var osc = new PulseWaveOscillator(sampleRate: 44100, phase: 0.0f);

            Assert.That(osc.SetPhase(1.75f), Is.EqualTo(0.75f).Within(1e-6f));
        }

        [Test]
        public void SetPhase_AffectsNextSampleOutput()
        {
            var osc = new PulseWaveOscillator(sampleRate: 44100, phase: 0.0f);
            osc.SetPhase(0.75f); // above 0.5 duty cycle

            Assert.That(osc.NextSample(frequency: 440, dutyCycle: 0.5f), Is.EqualTo(-1.0f));
        }
    }
}
