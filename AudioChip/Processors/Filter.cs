using AudioChip.Storage;

namespace AudioChip.Processors
{
    internal class Filter(int sampleRate)
    {
        public void SetFilter(Storage.Filter filter)
        {
            _type = filter.Type;
            SetCutoff(filter.Cutoff);
            switch (filter.Slope)
            {
                case FilterSlope.Slope12dB:
                    _filterStrategy = new SinglePoleStrategy();
                    break;
                case FilterSlope.Slope24dB:
                    _filterStrategy = new TwoPoleStrategy();
                    break;
            }
        }

        public void SetCutoff(int cutoff)
        {
            if (cutoff == _cutoff) return; // No need to recalculate if cutoff hasn't changed
            _cutoff = cutoff;

            var w0 = TwoPi * cutoff / sampleRate;
            var cosW0 = MathF.Cos(w0);
            var alpha = MathF.Sin(w0) / (2f * Q);
            switch (_type) // This could also be a strategy pattern if we wanted to avoid the switch, change if needed
            {
                case FilterType.LowPass:
                    SetLowPassCoefficients(cosW0, alpha);
                    break;
                case FilterType.HighPass:
                    SetHighPassCoefficients(cosW0, alpha);
                    break;
            }
        }

        public void Reset() => _filterStrategy.Reset();

        public float Apply(float input) => _filterStrategy.Apply(input, _biquadCoefficients);

        private void SetLowPassCoefficients(float cosW0, float alpha)
        {
            var invA0 = 1 / (1f + alpha);
            _biquadCoefficients = new BiquadCoefficients
            {
                b0 = (1f - cosW0) * 0.5f * invA0,
                b1 = (1f - cosW0) * invA0,
                b2 = (1f - cosW0) * 0.5f * invA0,
                a1 = -2f * cosW0 * invA0,
                a2 = (1f - alpha) * invA0
            };
        }

        private void SetHighPassCoefficients(float cosW0, float alpha)
        {
            var invA0 = 1 / (1f + alpha);
            _biquadCoefficients = new BiquadCoefficients
            {
                b0 = (1f + cosW0) * 0.5f * invA0,
                b1 = -(1f + cosW0) * invA0,
                b2 = (1f + cosW0) * 0.5f * invA0,
                a1 = -2f * cosW0 * invA0,
                a2 = (1f - alpha) * invA0,
            };
        }

        private int _cutoff;
        private FilterType _type;
        private BiquadCoefficients _biquadCoefficients;
        private IApplyFilterStrategy _filterStrategy = new SinglePoleStrategy();
        private const float Q = 0.7071f;  // Butterworth (maximally flat passband)
        private const float TwoPi = 2f * MathF.PI;

        private struct BiquadCoefficients
        {
            public float b0, b1, b2, a1, a2;
        }

        private interface IApplyFilterStrategy
        {
            void Reset();
            float Apply(float input, BiquadCoefficients coefficients);
        }

        private class SinglePoleStrategy : IApplyFilterStrategy
        {
            public void Reset()
            {
                z1 = z2 = 0f; // Clear biquad state
            }
            public float Apply(float input, BiquadCoefficients coefficients)
            {
                var filtered = coefficients.b0 * input + z1;
                z1 = coefficients.b1 * input - coefficients.a1 * filtered + z2;
                z2 = coefficients.b2 * input - coefficients.a2 * filtered;
                return filtered;
            }

            private float z1, z2; // State variables for single-pole filter
        }

        private class TwoPoleStrategy : IApplyFilterStrategy
        {
            public void Reset()
            {
                _firstPole.Reset();
                _secondPole.Reset();
            }

            public float Apply(float input, BiquadCoefficients coefficients)
            {
                var filtered = _firstPole.Apply(input, coefficients);
                filtered = _secondPole.Apply(filtered, coefficients);
                return filtered;
            }
            private SinglePoleStrategy _firstPole = new();
            private SinglePoleStrategy _secondPole = new();
        }
    }
}
