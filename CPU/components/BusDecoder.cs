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
            if (address >= MmioBase && address <= MmioEnd)
            {
                var offset = (byte)(address - MmioBase);
                value = _mmioDevice.ReadRegister(offset);
            }
            else if (address < MmioBase)
            {
                value = _memory.ReadByte(address);
            }
            else
            {
                value = 0;
            }
            Recorder?.RecordRead(address, value, BusType.Memory);
            return value;
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address >= MmioBase && address <= MmioEnd)
            {
                var offset = (byte)(address - MmioBase);
                _mmioDevice.WriteRegister(offset, value);
            }
            else if (address < MmioBase)
            {
                _memory.WriteByte(address, value);
            }
            // Reserved zone (stack): silently ignored
            Recorder?.RecordWrite(address, value, BusType.Memory);
        }

        internal const int MmioRegionSize = 256;
        private const ushort MmioBase = 0xEF00;
        private const ushort MmioEnd = 0xEFFF;
#else
        public byte ReadByte(byte address)
        {
            byte value;
            if (address >= MmioBase && address <= MmioEnd)
            {
                var offset = (byte)(address - MmioBase);
                value = _mmioDevice.ReadRegister(offset);
            }
            else if (address < MmioBase)
            {
                value = _memory.ReadByte(address);
            }
            else
            {
                value = 0;
            }
            Recorder?.RecordRead(address, value, BusType.Memory);
            return value;
        }

        public void WriteByte(byte address, byte value)
        {
            if (address >= MmioBase && address <= MmioEnd)
            {
                var offset = (byte)(address - MmioBase);
                _mmioDevice.WriteRegister(offset, value);
            }
            else if (address < MmioBase)
            {
                _memory.WriteByte(address, value);
            }
            // Reserved zone (stack): silently ignored
            Recorder?.RecordWrite(address, value, BusType.Memory);
        }

        internal const int MmioRegionSize = 8;
        private const byte MmioBase = 0xE8;
        private const byte MmioEnd = 0xEF;
#endif

        private readonly Memory _memory = memory;
        private readonly IMmioDevice _mmioDevice = mmioDevice;
    }
}
