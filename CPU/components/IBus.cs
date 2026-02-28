namespace CPU.components
{
#if x16
    public interface IBus
    {
        byte ReadByte(ushort address);
        void WriteByte(ushort address, byte value);
    }
#else
    public interface IBus
    {
        byte ReadByte(byte address);
        void WriteByte(byte address, byte value);
    }
#endif
}
