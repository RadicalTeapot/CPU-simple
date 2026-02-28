using CPU.microcode;

namespace CPU.components
{
    internal class BusDecoder(Memory memory, IMmioDevice mmioDevice) : IBus
    {
        internal BusRecorder? Recorder { get; set; }

#if x16
        public byte ReadByte(ushort address)
        {
            byte value;
            if (address >= MmioBase)
            {
                var offset = (byte)(address - MmioBase);
                value = _mmioDevice.ReadRegister(offset);
            }
            else
            {
                value = _memory.ReadByte(address);
            }
            Recorder?.RecordRead(address, value, BusType.Memory);
            return value;
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address >= MmioBase)
            {
                var offset = (byte)(address - MmioBase);
                _mmioDevice.WriteRegister(offset, value);
            }
            else
            {
                _memory.WriteByte(address, value);
            }
            Recorder?.RecordWrite(address, value, BusType.Memory);
        }

        private const ushort MmioBase = 0xFF00;
#else
        public byte ReadByte(byte address)
        {
            byte value;
            if (address >= MmioBase)
            {
                var offset = (byte)(address - MmioBase);
                value = _mmioDevice.ReadRegister(offset);
            }
            else
            {
                value = _memory.ReadByte(address);
            }
            Recorder?.RecordRead(address, value, BusType.Memory);
            return value;
        }

        public void WriteByte(byte address, byte value)
        {
            if (address >= MmioBase)
            {
                var offset = (byte)(address - MmioBase);
                _mmioDevice.WriteRegister(offset, value);
            }
            else
            {
                _memory.WriteByte(address, value);
            }
            Recorder?.RecordWrite(address, value, BusType.Memory);
        }

        private const byte MmioBase = 0xF0;
#endif

        private readonly Memory _memory = memory;
        private readonly IMmioDevice _mmioDevice = mmioDevice;
    }
}
