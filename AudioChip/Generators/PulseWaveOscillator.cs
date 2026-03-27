namespace AudioChip.Generators
{
    internal class PulseWaveOscillator(int sampleRate, float phase)
    {
        public float SetPhase(float newPhase)
        {
            _phase = newPhase % 1.0f;
            return _phase;
        }

        public float NextSample(int frequency, float dutyCycle)
        {
            var sample = _phase < dutyCycle ? 1.0f : -1.0f;
            _phase += (float)frequency / sampleRate;
            if (_phase >= 1.0f) _phase -= 1.0f;
            return sample;
        }

        private float _phase = phase;
    }
}