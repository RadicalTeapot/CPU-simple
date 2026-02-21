namespace CPU
{
    internal static class ByteConversionHelper
    {
        public static byte[] GetBytes(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        public static ushort ToUShort(byte high, byte low)
        {
            byte[] bytes = BitConverter.IsLittleEndian
                ? [low, high]
                : [high, low];
            return BitConverter.ToUInt16(bytes);
        }
    }
}
