namespace CPU.microcode
{
    internal class BusRecorder
    {
        public BusAccess? LastAccess { get; private set; }

        public void RecordRead(int address, byte data)
        {
            LastAccess = new BusAccess(address, data, BusDirection.Read);
        }

        public void RecordWrite(int address, byte data)
        {
            LastAccess = new BusAccess(address, data, BusDirection.Write);
        }

        public void Clear()
        {
            LastAccess = null;
        }
    }
}
