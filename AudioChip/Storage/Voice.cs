namespace AudioChip.Storage
{
    internal class Voice(int frequency, bool gate, Envelope envelope, Filter filter, float pulseWidth)
    {
        public int Frequency { get; set; } = frequency;
        public bool Gate { get; set; } = gate;
        public Envelope Envelope { get; set; } = envelope;
        public Filter Filter { get; set; } = filter;
        public float PulseWidth { get; set; } = pulseWidth;

        public static Voice Default => new(440, false, Envelope.Default, Filter.Default, 0.5f);

        public static (int frequency, bool gate) GetNoteAndGate(byte value)
        {
            var note = (byte)(value & 0x7F); // 7 bits for note
            var gate = (value & 0x80) != 0; // MSB for gate
            var frequency = ConversionHelpers.NoteToFrequency(note);
            return (frequency, gate);
        }

        public static float GetPulseWidth(byte value)
        {
            var pw = (value >> 4) & 0x03; // 2 bits for pulse width
            return pw switch
            {
                0 => 0.0625f,// 6.25%
                1 => 0.125f, // 12.5%
                2 => 0.25f,  // 25%
                3 => 0.5f,   // 50%
                _ => 0.5f    // Default to 50% if out of range
            };
    }
    }
}
