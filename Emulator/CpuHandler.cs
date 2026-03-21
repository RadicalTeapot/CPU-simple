using CPU;
using CPU.components;
using CPU.opcodes;
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
using PPU.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                    mmioRouter.Register(0x03, 0x01, _controller.Registers);
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

        public void TickFrame()
        {
            if (_ppuConfig == null) return;

            int totalPpuCycles = _ppuConfig.TotalScanlines * _ppuConfig.CyclesPerScanline;
            int ppuCyclesRun = 0;

            while (ppuCyclesRun < totalPpuCycles)
            {
                ppuCyclesRun += Tick();
                if (_currentState is IdleState or HaltedState or ErrorState)
                    break;
            }

            // If CPU stopped early, finish remaining PPU ticks so FrameReady still fires.
            if (ppuCyclesRun < totalPpuCycles && _ppu != null)
            {
                int remaining = totalPpuCycles - ppuCyclesRun;
                for (int i = 0; i < remaining; i++)
                    _ppu.Tick();
            }
        }

        /// <summary>
        /// Steps the CPU, ticks the PPU emulation to maintain synchronization, and updates the internal CPU state accordingly.
        /// </summary>
        /// <remarks>If the CPU encounters a HALT instruction or an error during execution, the internal
        /// state is updated to reflect the halted or error condition, and no PPU cycles are executed for that
        /// tick.</remarks>
        /// <returns>The number of PPU cycles executed during this tick. Returns 0 if the CPU is halted or an error occurs.</returns>
        public int Tick()
        {
            ICpuState nextState;
            int ppuCycles;
            try
            {
                nextState = _currentState.Tick();
                ppuCycles = TickPpu();
            }
            catch (OpcodeException.HaltException)
            {
                _logger.Log("CPU reached HALT instruction.");
                nextState = _cpuStateFactory.CreateHaltedState();
                ppuCycles = 0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failure during CPU tick: {ex.Message}");
                nextState = _cpuStateFactory.CreateErrorState(ex.Message);
                ppuCycles = 0;
            }
            _currentState = nextState;
            return ppuCycles;
        }

        /// <summary>
        /// Advances the PPU state by a number of ticks based on the current CPU execution
        /// state.
        /// </summary>
        /// <remarks>The number of PPU ticks is determined by the current CPU state and a configured tick
        /// ratio. This method should be called in synchronization with CPU execution to maintain accurate emulation
        /// timing.</remarks>
        /// <returns>The total number of PPU ticks performed. Returns 0 if the PPU is not initialized.</returns>
        private int TickPpu()
        {
            if (_ppu == null) return 0;

            int microTicks = _currentState is ExecutingCpuState
                ? Math.Max(1, _cpu.GetInspector().Traces.Length) // Relying on traces lenght to determine how many micro-operations were performed in the last CPU tick is quite hacky, find a more robust solution in the future.
                : 1;
            int count = microTicks * _ppuTickRatio;
            for (int i = 0; i < count; i++)
                _ppu.Tick();
            return count;
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
