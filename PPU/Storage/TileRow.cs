namespace PPU.Storage
{
    internal static class TileRow
    {
        public static bool GetPixel(byte rowData, int tileCol, bool hFlip)
        {
            int bitIndex = hFlip ? tileCol : (7 - tileCol); // MSB is leftmost pixel, LSB is rightmost pixel
            int bit = (rowData >> bitIndex) & 1;
            return bit != 0;
        }
    }
}
