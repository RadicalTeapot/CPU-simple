namespace AudioChip.Generators
{
    internal class Envelope(int sampleRate)
    {
        public void Reset() 
        {
            _phase = 0f;
        }

        public float NextSample(float attack, float release, float min, float max, bool gate)
        {
            var sample = GetValue(attack, release, gate);
            return sample * (max - min) + min; // Scale to desired range
        }

        private float GetValue(float attack, float release, bool gate)
        {
            if (gate)
            {
                if (_phase < attack) // Attack phase
                {
                    _phase += 1f / (attack * sampleRate);
                    return MathF.Min((_phase / attack), 1f);
                }
                return 1f; // Sustain phase
            }

            if (_phase < (attack + release)) // Release phase
            {
                _phase += 1f / (release * sampleRate);
                return MathF.Max(1 - (_phase - attack) / release, 0f);
            }
            return 0f;
        }

        private float _phase;
    }
}
