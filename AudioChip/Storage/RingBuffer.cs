using System;
namespace AudioChip.Storage
{
    internal class RingBuffer
    {
        public int AvailableSamples => _count;
        public int FreeSpace => _buffer.Length - _count;

        public RingBuffer(int size, int writeIndex = 0)
        {
            _buffer = new float[size];
            Array.Fill(_buffer, 0.0f);
            _writeIndex = writeIndex;
        }

        public void Write(float sample)
        {
            if (_count < _buffer.Length)
            {
                _buffer[_writeIndex] = sample;
                _writeIndex = (_writeIndex + 1) % _buffer.Length;
                _count++;
            }
        }

        public float Read()
        {
            if (_count > 0)
            {
                var sample = _buffer[_readIndex];
                _readIndex = (_readIndex + 1) % _buffer.Length;
                _count--;
                return sample;
            }
            return 0.0f;
        }

        private readonly float[] _buffer;
        private int _writeIndex;
        private int _readIndex;
        private int _count;
    }
}