using Emulator.Commands.GlobalCommands;
using Emulator.Commands.StateCommands;
using Emulator.CpuStates;
using Emulator.IO;
using CPU;
using CPU.components;
using CPU.opcodes;
using Controller.Storage;
using PPU.Configuration;
using System.Diagnostics;

namespace Emulator
{
    internal class CpuHandler
    {
        public record CpuHandlerContext(
            CPU.Config CpuConfig,
            ILogger Logger,
            IOutput Output,
            StateCommandRegistry CpuCommandRegistry,
            PeripheralSet? Peripherals = null,
            IReadOnlyList<byte>? ProgData = null
        );

        public event Action<IReadOnlyList<byte>>? FrameReady;
        public ButtonsState? ButtonState => _controller?.ButtonState;

        public CpuHandler(CpuHandlerContext context)
        {
            _logger = context.Logger;
            _output = context.Output;
            if (context.Peripherals == null)
            {
                _cpu = new CPU.CPU(context.CpuConfig);
            }
            else
            {
                var mmioRouter = new MmioRouter();
                _ppuConfig = context.Peripherals.PpuConfig;
                _ppuTickRatio = _ppuConfig.PpuCyclesPerCpuCycle;
                _ppu = new PPU.Ppu(_ppuConfig, context.Peripherals.ChrData);
                mmioRouter.Register(0x00, 0x03, _ppu.Registers);

                if (context.Peripherals.ControllerConfig != null)
                {
                    _controller = new Controller.Controller(context.Peripherals.ControllerConfig);
                    mmioRouter.Register(0x04, 0x01, _controller.Registers);
                }

                _cpu = new CPU.CPU(context.CpuConfig, mmioRouter);
                _ppu.VBlankStarted += _cpu.RequestInterrupt;
                _ppu.FrameReady += rgb => FrameReady?.Invoke(rgb);
            }

            if (context.ProgData != null)
            {
                _cpu.LoadProgram([.. context.ProgData]);
                _cpu.Reset();
            }

            _breakpointContainer = new BreakpointContainer();
            _watchpointContainer = new WatchpointContainer();
            _cpuStateFactory = new CpuStateFactory(_cpu, _logger, _output, _breakpointContainer, _watchpointContainer, context.CpuCommandRegistry);
            _currentState = context.ProgData == null
                ? _cpuStateFactory.CreateIdleState()
                : _cpuStateFactory.CreateRunningState(new Run.Config(Run.Mode.ToHalt, 0));
        }

        public void HandleGlobalCommand(IGlobalCommand globalCommand, string[] args)
        {
            var inspector = _cpu.GetInspector();
            var context = new GlobalCommandExecutionContext(inspector, _currentState, _breakpointContainer, _watchpointContainer, _output);

            var result = globalCommand.Execute(context, args);
            if (!result.Success)
            {
                _logger.Error(result.Message ?? $"Global command '{globalCommand.Name} 'failed to execute.");
            }
            else if (!string.IsNullOrEmpty(result.Message))
            {
                _logger.Log(result.Message);
            }
        }

        public void HandleStateCommand(IStateCommand cpuCommand, string[] args)
        {
            _currentState = _currentState.GetStateForCommand(cpuCommand, args);
        }

        public void Tick()
        {
            ICpuState nextState;
            try
            {
                nextState = _currentState.Tick();
                TickPpu();
            }
            catch (OpcodeException.HaltException)
            {
                _logger.Log("CPU reached HALT instruction.");
                nextState = _cpuStateFactory.CreateHaltedState();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failure during CPU tick: {ex.Message}");
                nextState = _cpuStateFactory.CreateErrorState(ex.Message);
            }
            _currentState = nextState;
        }

        public void TickFrame()
        {
            if (_ppuConfig == null) return;

            int stateTickBudget = (_ppuConfig.TotalScanlines * _ppuConfig.CyclesPerScanline)
                                / _ppuTickRatio;
            int ticksExecuted = 0;
            for (int i = 0; i < stateTickBudget; i++)
            {
                Tick();
                ticksExecuted++;
                if (_currentState is IdleState or HaltedState or ErrorState)
                    break;
            }

            // If CPU stopped early, finish remaining PPU ticks to complete the frame
            int ppuTicksDone = ticksExecuted * _ppuTickRatio; // approximate, already ticked in Tick()
            // We accounted for PPU ticks inside Tick(), but if we stopped early the frame isn't done.
            // Tick remaining PPU cycles so FrameReady fires even if CPU halts mid-frame.
            int totalPpuTicksNeeded = _ppuConfig.TotalScanlines * _ppuConfig.CyclesPerScanline;
            // ppuTicksDone is approximate via TickPpu() calls inside Tick()
            // For simplicity, just tick the remaining scanlines worth of PPU cycles
            if (ticksExecuted < stateTickBudget && _ppu != null)
            {
                int remaining = totalPpuTicksNeeded - (ticksExecuted * _ppuTickRatio);
                for (int i = 0; i < remaining; i++)
                    _ppu.Tick();
            }
        }

        private void TickPpu()
        {
            if (_ppu == null) return;

            int microTicks = _currentState is ExecutingCpuState
                ? Math.Max(1, _cpu.GetInspector().Traces.Length)
                : 1;
            for (int i = 0; i < microTicks * _ppuTickRatio; i++)
                _ppu.Tick();
        }

        private ICpuState _currentState;
        private readonly CPU.CPU _cpu;

        private readonly PPU.Ppu? _ppu;
        private readonly PpuConfig? _ppuConfig;
        private readonly int _ppuTickRatio;

        private readonly Controller.Controller? _controller;

        private readonly CpuStateFactory _cpuStateFactory;
        private readonly ILogger _logger;
        private readonly IOutput _output;
        private readonly BreakpointContainer _breakpointContainer;
        private readonly WatchpointContainer _watchpointContainer;
    }
}
