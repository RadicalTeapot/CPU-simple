using CPU.components;

namespace PPU
{
    public class Ppu
    {
        public IMmioDevice Registers => _registers;

        public event Action? VBlankStarted;

        public Ppu(int vramSize)
        {
            _registers = new PpuRegisters(vramSize);
        }

        public void Tick()
        {
            _scanlineCycle++;
            if (_scanlineCycle >= CyclesPerScanline)
            {
                _scanlineCycle = 0;
                _scanline++;
                if (_scanline == VBlankScanline)
                {
                    _registers.VBlankActive = true;
                    VBlankStarted?.Invoke();
                }
                else if (_scanline >= TotalScanlines)
                {
                    _scanline = 0;
                }
            }
        }

        private const int CyclesPerScanline = 341;
        private const int VBlankScanline = 241;
        private const int TotalScanlines = 262;

        private int _scanline;
        private int _scanlineCycle;
        private readonly PpuRegisters _registers;
    }
}
