namespace AudioChip.Storage
{
    internal class Envelope(byte attack, byte sustain, byte release)
    {
        public byte Attack { get; set; } = attack;
        public byte Sustain { get; set; } = sustain;
        public byte Release { get; set; } = release;

        public static Envelope Default => new(0x00, 0xFF, 0x00);

        public static (byte attack, byte release) GetAttackRelease(byte value)
        {
            byte attack = (byte)(value & 0x0F);
            byte release = (byte)((value >> 4) & 0x0F);
            return (attack, release);
        }

        public static byte GetSustain(byte value)
        {
            return (byte)(value & 0x0F);
        }
    }
}
