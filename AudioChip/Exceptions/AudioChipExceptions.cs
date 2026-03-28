namespace AudioChip.Exceptions
{
    internal class AudioChipExceptions : Exception
    {
        public AudioChipExceptions(string message) : base(message) { }
        public AudioChipExceptions(string message, Exception innerException) : base(message, innerException) { }

        public class BufferOverrunException : AudioChipExceptions
        {
            public BufferOverrunException() : base("Audio buffer overrun: too many samples generated without being read.") { }
        }

        public class BufferUnderrunException : AudioChipExceptions
        {
            public BufferUnderrunException() : base("Audio buffer underrun: attempted to read more samples than available.") { }
        }
    }
}
