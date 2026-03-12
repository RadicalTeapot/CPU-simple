using Backend.Commands.GlobalCommands;
using Backend.Commands.StateCommands;
using Backend.IO;
using PPU.Configuration;
using System.Diagnostics;

namespace Backend
{
    public class BackendApplication
    {
        public BackendApplication(ILogger logger, IInput input, IOutput output, CPU.Config cpuConfig,
            PpuConfig? ppuConfig = null, int displayScale = 4)
        {
            _logger = logger;
            _output = output;

            _globalCommandRegistry = new GlobalCommandRegistry();
            _stateCommandRegistry = new StateCommandRegistry();
            _cpuHandler = new CpuHandler(cpuConfig, output, logger, _stateCommandRegistry);
            _commandReader = new CommandReader(input, _logger);

            if (ppuConfig != null)
            {
                _display = new Display(ppuConfig.ScreenWidth, ppuConfig.ScreenHeight, displayScale);
                _cpuHandler.FrameReady += _display.UpdateFrame;
            }
        }

        public int Run()
        {
            _logger.Log("Backend application started.");
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
            while (!_display!.ShouldClose && !_quitRequested)
            {
                if (DrainCommands()) break;
                _output.StatusSuppressed = true;
                _cpuHandler.TickFrame();
                _output.StatusSuppressed = false;
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
            _logger.Log("Quitting backend application.");
            _commandReader.StopReader();
            _display?.Dispose();
        }

        private bool TryParseCommand(out ParsedCommand? parsedCommand)
        {
            parsedCommand = default;
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
