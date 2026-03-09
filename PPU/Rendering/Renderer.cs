using PPU.Configuration;
using PPU.Storage;

namespace PPU.Rendering
{
    internal class Renderer(PpuConfig config, Vram vram, ChrRom rom, PpuRegisters registers)
    {
        public void BeginFrame()
        {
            _buffer.Clear();
        }

        public void RenderScanline(int scanlineIndex)
        {
            if (scanlineIndex % config.TileSize == 0)
            {
                _nextScanlineSprites = _spriteEvaluator.Evaluate(scanlineIndex, registers);
            }
            _scanlineRenderer.RenderScanline(scanlineIndex, _nextScanlineSprites, _buffer);
        }

        private OamEntry[] _nextScanlineSprites = [];

        private readonly Framebuffer _buffer = new(config);
        private readonly SpriteEvaluator _spriteEvaluator = new(vram, config);
        private readonly ScanlineRenderer _scanlineRenderer = new(config, vram, rom);
    }
}
