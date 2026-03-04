using PPU.Configuration;

namespace PPU.Storage
{
    public class Vram(PpuConfig config)
    {
        public int Size { get => _vram.Length; }

        public byte Read(int address)
        {
            if (address < 0 || address >= _vram.Length)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of VRAM bounds.");

            return _vram[address];
        }

        public void Write(int address, byte value)
        {
            if (address < 0 || address >= _vram.Length)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of VRAM bounds.");
            
            _vram[address] = value;
        }

        private readonly byte[] _vram = new byte[config.VramLayout.TotalSize];
    }
}
