namespace AudioChip.Storage
{
    /// <summary>
    /// Represents an ASR (Attack, Sustain, Release) envelope used to shape the amplitude of a sound over time.
    /// </summary>
    /// <param name="attack">The attack time, in seconds, specifying how long it takes for the envelope to reach its maximum level after a
    /// note is triggered. Must be non-negative.</param>
    /// <param name="sustain">The sustain level, as a value between 0.0 and 1.0, indicating the level at which the envelope holds after the
    /// attack phase until the note is released.</param>
    /// <param name="release">The release time, in seconds, specifying how long it takes for the envelope to return to zero after a note is
    /// released. Must be non-negative.</param>
    internal class Envelope(float attack, float sustain, float release)
    {
        /// <summary>
        /// Attack time in seconds. The time it takes for the envelope to reach its maximum level after a note is triggered.
        /// </summary>
        public float Attack { get; set; } = attack;
        /// <summary>
        /// Sustain level as a value between 0.0 and 1.0. The level at which the envelope holds after the attack phase until the note is released.
        /// </summary>
        public float Sustain { get; set; } = sustain;
        /// <summary>
        /// Release time in seconds. The time it takes for the envelope to return to zero after a note is released.
        /// </summary>
        public float Release { get; set; } = release;

        public static Envelope Default => new(0f, 1f, 0f);

        public static (float attack, float release) GetAttackRelease(byte value)
        {
            byte attack = (byte)(value & 0x0F);
            byte release = (byte)((value >> 4) & 0x0F);
            return (AttackRates[attack], ReleaseRates[release]);
        }

        public static float GetSustain(byte value)
        {
            return (float)(value & 0x0F) / 0x0F;
        }

        // Lookup table for attack rates, in seconds, based on 4-bit attack value (0-15)
        // These values are the same as the one used by the SID chip
        private static readonly float[] AttackRates = [
            0.002f, 0.008f, 0.016f, 0.024f, 0.038f, 0.056f, 0.068f, 0.080f,
            0.1f, 0.24f, 0.5f, 0.8f, 1.0f, 3.0f, 5.0f, 8.0f
        ];

        // Lookup table for release rates, in seconds, based on 4-bit release value (0-15)
        // These values are the same as the one used by the SID chip
        private static readonly float[] ReleaseRates = [
            0.006f, 0.024f, 0.048f, 0.072f, 0.114f, 0.168f, 0.204f, 0.24f,
            0.3f, 0.75f, 1.5f, 2.4f, 3.0f, 9.0f, 15.0f, 24.0f
        ];
    }
}
