namespace AudioChip.Storage
{
    internal enum FilterType
    {
        LowPass,
        HighPass
    };

    internal enum FilterSlope
    {
        Slope12dB,
        Slope24dB
    };

    internal class Filter(int cutoff, FilterType type, FilterSlope slope, bool useEnvelope)
    {
        public int Cutoff { get; set; } = cutoff;
        public FilterType Type { get; set; } = type;
        public FilterSlope Slope { get; set; } = slope;
        public bool UseEnvelope { get; set; } = useEnvelope;

        public static Filter Default => new(20000, FilterType.LowPass, FilterSlope.Slope12dB, false);

        public static (int cutoff, bool useEnv) GetFilterParameters(byte value)
        {
            var note = (byte)(value & 0x7F); // 7 bits for note
            var cutoff = ConversionHelpers.NoteToFrequency(note);
            var useEnv = (value & 0x80) != 0;
            return (cutoff, useEnv);
        }

        public static (FilterType type, FilterSlope slope) GetFilterTypeAndSlope(byte value)
        {
            var type = (value & 0x40) == 0 ? FilterType.LowPass : FilterType.HighPass;
            var slope = (value & 0x80) == 0 ? FilterSlope.Slope12dB : FilterSlope.Slope24dB;
            return (type, slope);
        }
    }
}
