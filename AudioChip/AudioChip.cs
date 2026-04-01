using AudioChip.Configuration;
using AudioChip.Exceptions;
using AudioChip.Storage;
using CPU.components;

namespace AudioChip
{
    public class AudioChip
    {
        public IMmioDevice Registers => _registers;
        public int AvailableSamples => _ringBuffer.AvailableSamples;

        public AudioChip(AudioConfiguration configuration) 
        {
            var voice = Voice.Default;
            _registers = new AudioRegisters(voice);
            _audioChain = new AudioChain(configuration, voice);
            _ringBuffer = new RingBuffer(configuration.BufferSize * 4, configuration.BufferSize * 2); // Shift write index to create space for initial samples
            _samplesPerTick = (float)configuration.SampleRate / configuration.CpuClockRate;
        }

        public void Tick()
        {
            var samplesToGenerate = (int)(_samplesPerTick + _fractionalSamplePosition);
            _fractionalSamplePosition += _samplesPerTick - samplesToGenerate;
            var writableSamples = Math.Min(samplesToGenerate, _ringBuffer.FreeSpace);
            // FIXME This fails at app initialization
            // if (writableSamples <= 0)
            // {
            //     throw new AudioChipExceptions.BufferOverrunException();
            // }

            for (int i = 0; i < writableSamples; i++)
            {
                var sample = _audioChain.GetSample();
                _ringBuffer.Write(sample);
            }
        }

        public Span<float> ConsumeSamples(int count)
        {
            if (count > _ringBuffer.AvailableSamples)
            {
                throw new AudioChipExceptions.BufferUnderrunException();
            }

            var samplesToRead = Math.Min(count, _ringBuffer.AvailableSamples);
            var output = new float[samplesToRead];
            for (int i = 0; i < samplesToRead; i++)
            {
                output[i] = _ringBuffer.Read();
            }
            return output;
        }

        private float _fractionalSamplePosition;

        private readonly AudioRegisters _registers;
        private readonly AudioChain _audioChain;
        private readonly RingBuffer _ringBuffer;
        private readonly float _samplesPerTick;
    }
}
