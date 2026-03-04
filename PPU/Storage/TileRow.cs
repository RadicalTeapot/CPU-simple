namespace PPU.Storage
{
    internal static class TileRow
    {
        public static bool GetPixel(byte row, int pixelX, bool hFlip)
        {
            int bitIndex = hFlip ? pixelX : (7 - pixelX);
            int bit = (row >> bitIndex) & 1;
            return bit != 0;
        }
    }
}
