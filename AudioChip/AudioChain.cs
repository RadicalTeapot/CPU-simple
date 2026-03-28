using AudioChip.Configuration;
using AudioChip.Generators;
using AudioChip.Processors;
using AudioChip.Storage;

namespace AudioChip
{
    internal class AudioChain(AudioConfiguration configuration, Voice voice)
    {
        public float GetSample()
        {
            var osc = _pulseWaveOscillator.NextSample(voice.Frequency, voice.PulseWidth);
            var filterEnv = _filterEnvelope.NextSample(voice.Filter.Envelope.Attack, voice.Filter.Envelope.Release, 0.0f, voice.Filter.Cutoff, voice.Gate);
            _filter.SetCutoff((int)filterEnv, voice.Filter.Type);
            _filter.SetFilterSlope(voice.Filter.Slope);
            var filtered = _filter.Apply(osc);
            var oscEnv = _oscillatorEnvelope.NextSample(voice.Envelope.Attack, voice.Envelope.Release, 0.0f, voice.Envelope.Sustain, voice.Gate);
            var output = _vca.Apply(filtered, oscEnv);
            return Math.Clamp(output, -1.0f, 1.0f);
        }

        private readonly PulseWaveOscillator _pulseWaveOscillator = new(configuration.SampleRate, 0.0f);
        private readonly Processors.Filter _filter = new(configuration.SampleRate);
        private readonly Attenuator _vca = new();
        private readonly Generators.Envelope _oscillatorEnvelope = new(configuration.SampleRate);
        private readonly Generators.Envelope _filterEnvelope = new(configuration.SampleRate);
    }
}
