namespace AudioChip
{
    internal static class ConversionHelpers
    {
        public static int NoteToFrequency(byte note)
        {
            // A4 = 440 Hz, MIDI note 69
            return (int)(440.0 * Math.Pow(2, (note - 69) / 12.0));
        }
    }
}
