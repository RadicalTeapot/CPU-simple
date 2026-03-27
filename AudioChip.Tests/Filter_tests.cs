using AudioChip.Storage;
using ProcessorFilter = AudioChip.Processors.Filter;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class Filter_tests
    {
        private const int SampleRate = 44100;
        private const int Cutoff = 1000;

        [Test]
        public void Apply_LowPassFilter_DcSignalConvergesToInput()
        {
            // DC gain of a LPF biquad is 1; output should settle to the input value.
            var filter = CreateFilter(Cutoff, FilterType.LowPass, FilterSlope.Slope12dB);

            float output = 0;
            for (int i = 0; i < 1000; i++)
                output = filter.Apply(1.0f);

            Assert.That(output, Is.EqualTo(1.0f).Within(1e-3f));
        }

        [Test]
        public void Apply_HighPassFilter_DcSignalConvergesToZero()
        {
            // DC gain of a HPF biquad is 0; DC input should be blocked entirely.
            var filter = CreateFilter(Cutoff, FilterType.HighPass, FilterSlope.Slope12dB);

            float output = 1;
            for (int i = 0; i < 1000; i++)
                output = filter.Apply(1.0f);

            Assert.That(output, Is.EqualTo(0.0f).Within(1e-3f));
        }

        [Test]
        public void Apply_TwoPoleLowPassFilter_DcSignalConvergesToInput()
        {
            // Two poles in series have the same DC gain (1) but steeper roll-off;
            // DC should still converge to the input value.
            var filter = CreateFilter(Cutoff, FilterType.LowPass, FilterSlope.Slope24dB);

            float output = 0;
            for (int i = 0; i < 2000; i++)
                output = filter.Apply(1.0f);

            Assert.That(output, Is.EqualTo(1.0f).Within(1e-3f));
        }

        [Test]
        public void Reset_ClearsFilterState_OutputMatchesFreshFilter()
        {
            var filter = CreateFilter(Cutoff, FilterType.LowPass, FilterSlope.Slope12dB);

            // Record output for the very first sample (zero internal state)
            var firstOutput = filter.Apply(1.0f);

            // Let the filter build up internal state
            for (int i = 0; i < 100; i++)
                filter.Apply(0.5f);

            filter.Reset();

            // After reset the internal state is zero again; same input should give same output
            Assert.That(filter.Apply(1.0f), Is.EqualTo(firstOutput).Within(1e-6f));
        }

        private static ProcessorFilter CreateFilter(int cutoff, FilterType type, FilterSlope slope)
        {
            var filter = new ProcessorFilter(SampleRate);
            filter.SetFilter(new Storage.Filter(cutoff, type, slope, Envelope.Default));
            return filter;
        }
    }
}
