namespace CPU.components
{
    public class NullMmioDevice : IMmioDevice
    {
        public byte ReadRegister(byte offset) => 0;
        public void WriteRegister(byte offset, byte value) { }
    }
}
