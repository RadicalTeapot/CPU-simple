namespace CPU.microcode
{
    internal class BusRecorder
    {
        public BusAccess? LastAccess { get; private set; }

        public void RecordRead(int address, byte data, BusType busType)
        {
            LastAccess = new BusAccess(address, data, BusDirection.Read, busType);
        }

        public void RecordWrite(int address, byte data, BusType busType)
        {
            LastAccess = new BusAccess(address, data, BusDirection.Write, busType);
        }

        public void Clear()
        {
            LastAccess = null;
        }
    }
}
