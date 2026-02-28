namespace CPU.components
{
    public interface IMmioDevice
    {
        byte ReadRegister(byte offset);
        void WriteRegister(byte offset, byte value);
    }
}
