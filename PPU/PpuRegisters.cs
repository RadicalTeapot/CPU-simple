using CPU.components;

namespace PPU
{
    public class PpuRegisters(int vramSize) : IMmioDevice
    {
        public bool VBlankActive { get; set; }

        public byte ReadRegister(byte offset)
        {
            return offset switch
            {
                StatusOffset => ReadStatus(),
                _ => 0,
            };
        }

        public void WriteRegister(byte offset, byte value)
        {
            switch (offset)
            {
                case AddrOffset:
                    WriteAddr(value);
                    break;
                case DataOffset:
                    WriteData(value);
                    break;
            }
        }

        private byte ReadStatus()
        {
            byte status = 0;
            if (VBlankActive)
                status |= VBlankBit;
            VBlankActive = false;
            return status;
        }

        private void WriteAddr(byte value)
        {
            if (_useLatch)
            {
                if (_addrLatchHigh)
                {
                    _vramAddress = (ushort)(value << 8);
                    _addrLatchHigh = false;
                }
                else
                {
                    _vramAddress |= value;
                    _addrLatchHigh = true;
                }
            }
            else
            {
                _vramAddress = value;
            }
        }

        private void WriteData(byte value)
        {
            if (_vramAddress < _vram.Length)
                _vram[_vramAddress] = value;
            _vramAddress++;
        }

        private const byte AddrOffset = 0;
        private const byte DataOffset = 1;
        private const byte StatusOffset = 2;
        private const byte VBlankBit = 0x80;

        private bool _addrLatchHigh = true;
        private ushort _vramAddress;
        private readonly byte[] _vram = new byte[vramSize];
        private readonly bool _useLatch = vramSize > 256;
    }
}
