using CPU.components;
using PPU.Configuration;
using PPU.DebuggerInteraction;
using PPU.Rendering;
using PPU.Storage;

namespace PPU
{
    public class Ppu
    {
        public IMmioDevice Registers => _registers;
        public event Action? VBlankStarted;
        public event Action<IReadOnlyList<byte>>? FrameReady;

        public Ppu(PpuConfig config, ChrRom rom)
        {
            _config = config;
            _rgbBuffer = new byte[config.ScreenWidth * config.ScreenHeight * 3];

            var vram = new Vram(config);
            _registers = new PpuRegisters(vram);
            _renderer = new Renderer(config, vram, rom, _registers);

            // Setup the state machine for scanline ticking
            _vBlankState = new VBlankState(config, _registers);
            _renderState = new RenderState(config, _renderer);
            _vBlankState.VBlankStarted += OnVBlankStarted;
            _vBlankState.SetNextState(_renderState);
            _renderState.SetNextState(_vBlankState);
            _currentState = _renderState;
            _currentState.Enter();
        }

        public PpuTickResult Tick()
        {
            // Note that in the current implementation the very first scanline
            // will be blank as the state machine tick of the current scanline
            // is executed after the scanline cycle count reaches the configured cycles per scanline
            _scanlineCycle++;
            if (_scanlineCycle >= _config.CyclesPerScanline)
            {
                _scanlineCycle = 0;
                _currentState = _currentState.Tick();
            }
            return new PpuTickResult();
        }


        private void OnVBlankStarted()
        {
            VBlankStarted?.Invoke();
            ConvertFramebufferToRgb();
            FrameReady?.Invoke(_rgbBuffer);
        }

        private void ConvertFramebufferToRgb()
        {
            var pixels = _renderer.ReadOnlyPixels;
            for (int i = 0; i < pixels.Count; i++)
            {
                byte rgb = pixels[i] == 0 ? (byte)0 : (byte)255;
                int offset = i * 3;
                _rgbBuffer[offset] = rgb;
                _rgbBuffer[offset + 1] = rgb;
                _rgbBuffer[offset + 2] = rgb;
            }
        }

        private int _scanlineCycle;
        private IScanlineTickState _currentState;

        private readonly PpuConfig _config;
        private readonly PpuRegisters _registers;
        private readonly Renderer _renderer;
        private readonly byte[] _rgbBuffer;

        private readonly VBlankState _vBlankState;
        private readonly RenderState _renderState;

        private interface IScanlineTickState
        {
            void Enter();
            void SetNextState(IScanlineTickState nextState);
            IScanlineTickState Tick();
        }

        private class VBlankState(PpuConfig config, PpuRegisters registers) : IScanlineTickState
        {
            public event Action? VBlankStarted;

            public void Enter()
            {
                registers.VBlankActive = true;
                VBlankStarted?.Invoke();
                _scanline = 0;
            }

            public void SetNextState(IScanlineTickState nextState)
                => _nextState = nextState;

            public IScanlineTickState Tick()
            {
                if (_scanline >= config.VBlankScanlineCount)
                {
                    _nextState?.Enter();
                    return _nextState?.Tick() ?? throw new Exception();
                }
                _scanline++;
                return this;
            }

            private IScanlineTickState? _nextState;
            private int _scanline;
        }

        private class RenderState(PpuConfig config, Renderer renderer) : IScanlineTickState
        {
            public void Enter()
            {
                renderer.BeginFrame();
                _scanline = 0;
            }

            public void SetNextState(IScanlineTickState nextState)
                => _nextState = nextState;

            public IScanlineTickState Tick()
            {
                if (_scanline >= config.ScreenHeight)
                {
                    _nextState?.Enter();
                    return _nextState?.Tick() ?? throw new Exception();
                }
                renderer.RenderScanline(_scanline);
                _scanline++;
                return this;
            }

            private IScanlineTickState? _nextState;
            private int _scanline;
        }
    }
}
