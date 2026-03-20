using Controller.Configuration;
using Emulator.Commands.GlobalCommands;
using Emulator.Commands.StateCommands;
using Emulator.IO;
using PPU.Configuration;
using System.Diagnostics;

namespace Emulator
{
    // PPU is required; Controller is optional. Controller without PPU is not constructible.
    public record PeripheralSet(
        PpuConfig PpuConfig,
        IReadOnlyList<byte>? ChrData = null,
        ControllerConfiguration? ControllerConfig = null
    );

    public class EmulatorApplication
    {
        public record EmulatorContext(
            ILogger Logger,
            IInput Input,
            IOutput Output,
            CPU.Config CpuConfig,
            PeripheralSet? Peripherals = null,
            int DisplayScale = 4,
            IReadOnlyList<byte>? ProgData = null
        );

        public EmulatorApplication(EmulatorContext context)
        {
            _logger = context.Logger;
            _output = context.Output;

            _globalCommandRegistry = new GlobalCommandRegistry();
            _stateCommandRegistry = new StateCommandRegistry();
            var cpuHandlerContext = new CpuHandler.CpuHandlerContext(
                context.CpuConfig,
                context.Logger,
                context.Output,
                _stateCommandRegistry,
                context.Peripherals,
                context.ProgData
            );
            _cpuHandler = new CpuHandler(cpuHandlerContext);
            _commandReader = new CommandReader(context.Input, _logger);

            if (context.Peripherals == null)
                return;

            _display = new Display(context.Peripherals.PpuConfig.ScreenWidth, context.Peripherals.PpuConfig.ScreenHeight, context.DisplayScale);
            _cpuHandler.FrameReady += _display.UpdateFrame;
        }

        public int Run()
        {
            _logger.Log("Emulator application started.");
            _commandReader.StartReader();
            return _display != null ? RunWindowed() : RunHeadless();
        }

        private int RunHeadless()
        {
            while (true)
            {
                if (DrainCommands()) break;
                _cpuHandler.Tick();
                Thread.Sleep(100); // 10Hz, TODO make it configurable
            }
            Cleanup();
            return 0;
        }

        private int RunWindowed()
        {
            while (!Display.ShouldClose && !_quitRequested)
            {
                if (DrainCommands()) break;
                _output.StatusSuppressed = true;
                try
                {
                    _cpuHandler.TickFrame();
                }
                finally
                {
                    _output.StatusSuppressed = false; // Ensure that status output is re-enabled even if an exception occurs during TickFrame
                }
                _display.Render();
            }
            Cleanup();
            return 0;
        }

        /// <summary>
        /// Drains all pending commands from the command queue.
        /// Returns true if a quit command was received.
        /// </summary>
        private bool DrainCommands()
        {
            while (TryParseCommand(out var parsedCommand))
            {
                Debug.Assert(parsedCommand != null, "Parsed command shouldn't be null here");
                if (parsedCommand.Name == "quit" || parsedCommand.Name == "exit" || parsedCommand.Name == "q")
                {
                    _quitRequested = true;
                    return true;
                }

                if (_globalCommandRegistry.TryGetCommand(parsedCommand.Name, out var globalCommand))
                {
                    Debug.Assert(globalCommand != null);
                    _cpuHandler.HandleGlobalCommand(globalCommand, parsedCommand.Arguments);
                }
                else if (_stateCommandRegistry.TryGetCommand(parsedCommand.Name, out var stateCommand))
                {
                    Debug.Assert(stateCommand != null);
                    _cpuHandler.HandleStateCommand(stateCommand, parsedCommand.Arguments);
                }
                else
                {
                    _logger.Error($"Unknown command: {parsedCommand.Name}");
                }
            }
            return false;
        }

        private void Cleanup()
        {
            _logger.Log("Quitting emulator application.");
            _commandReader.StopReader();
            _display?.Dispose();
        }

        private bool TryParseCommand(out ParsedCommand? parsedCommand)
        {
            parsedCommand = null;
            if (!_commandReader.TryGetCommand(out var command)) return false;
            if (string.IsNullOrEmpty(command)) return false;

            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            parsedCommand = new ParsedCommand(
                Name: parts[0],
                Arguments: parts.Length > 1 ? parts[1..] : []);
            return true;
        }

        private record ParsedCommand(string Name, string[] Arguments) { }

        private bool _quitRequested;

        private readonly ILogger _logger;
        private readonly IOutput _output;

        private readonly Display? _display;
        private readonly CpuHandler _cpuHandler;
        private readonly GlobalCommandRegistry _globalCommandRegistry;
        private readonly StateCommandRegistry _stateCommandRegistry;
        private readonly CommandReader _commandReader;
    }
}
