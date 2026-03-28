using AudioChip.Generators;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class Envelope_tests
    {
        private const int SampleRate = 100;
        private const float Attack = 1.0f;  // 100 samples at SampleRate=100
        private const float Release = 1.0f; // 100 samples at SampleRate=100

        [Test]
        public void NextSample_AttackPhase_FirstValueIsAboveMinAndBelowMax()
        {
            var env = new Envelope(SampleRate);

            var first = env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);

            Assert.That(first, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void NextSample_AttackPhase_OutputIncreasesOverTime()
        {
            var env = new Envelope(SampleRate);
            var early = env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);

            for (int i = 0; i < 40; i++)
                env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);
            var later = env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);

            Assert.That(later, Is.GreaterThan(early));
        }

        [Test]
        public void NextSample_SustainPhase_ReturnsMax()
        {
            var env = new Envelope(SampleRate);
            for (int i = 0; i < SampleRate; i++)
                env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);

            Assert.That(env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true), Is.EqualTo(1.0f));
        }

        [Test]
        public void NextSample_SustainPhase_ScalesOutputToMax()
        {
            var env = new Envelope(SampleRate);
            for (int i = 0; i < SampleRate; i++)
                env.NextSample(Attack, Release, min: 0f, max: 0.5f, gate: true);

            Assert.That(env.NextSample(Attack, Release, min: 0f, max: 0.5f, gate: true), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void NextSample_ReleasePhase_FirstValueIsNearMax()
        {
            var env = new Envelope(SampleRate);
            for (int i = 0; i < SampleRate; i++)
                env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);

            var firstRelease = env.NextSample(Attack, Release, min: 0f, max: 1f, gate: false);

            Assert.That(firstRelease, Is.GreaterThan(0.5f));
        }

        [Test]
        public void NextSample_AfterReleaseCompletes_ReturnsMin()
        {
            var env = new Envelope(SampleRate);
            for (int i = 0; i < SampleRate; i++)
                env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);
            for (int i = 0; i < SampleRate * 2; i++)
                env.NextSample(Attack, Release, min: 0f, max: 1f, gate: false);

            Assert.That(env.NextSample(Attack, Release, min: 0f, max: 1f, gate: false), Is.EqualTo(0f));
        }

        [Test]
        public void NextSample_ZeroAttack_FirstGateSampleReturnsMax()
        {
            var env = new Envelope(SampleRate);

            var first = env.NextSample(attack: 0f, Release, min: 0f, max: 1f, gate: true);

            Assert.That(first, Is.EqualTo(1f));
        }

        [Test]
        public void NextSample_ZeroRelease_FirstReleaseSampleReturnsMin()
        {
            var env = new Envelope(SampleRate);
            for (int i = 0; i < SampleRate; i++)
                env.NextSample(Attack, release: 0f, min: 0f, max: 1f, gate: true);

            var first = env.NextSample(Attack, release: 0f, min: 0f, max: 1f, gate: false);

            Assert.That(first, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_RestartsAttackFromBeginning()
        {
            var env = new Envelope(SampleRate);
            for (int i = 0; i < SampleRate; i++)
                env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);

            env.Reset();

            var firstAfterReset = env.NextSample(Attack, Release, min: 0f, max: 1f, gate: true);
            Assert.That(firstAfterReset, Is.LessThan(0.1f));
        }
    }
}
