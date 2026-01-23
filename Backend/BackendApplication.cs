using Backend.Commands.GlobalCommands;
using Backend.Commands.StateCommands;
using Backend.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Backend
{
    public class BackendApplication
    {
        public BackendApplication(ILogger logger, IInput input, IOutput output, CPU.Config cpuConfig)
        {
            _logger = logger;
            _input = input;
            _output = output;

            _globalCommandRegistry = new GlobalCommandRegistry();
            _stateCommandRegistry = new StateCommandRegistry();
            _cpuHandler = new CpuHandler(cpuConfig, output, logger, _stateCommandRegistry);
            _commandReader = new CommandReader(_input, _logger);
        }

        public int Run()
        {
            _logger.Log("Backend application started.");
            _commandReader.StartReader();

            while (true)
            {
                if (_commandReader.TryGetCommand(out var command))
                {
                    Debug.Assert(command != null, "Command shouldn't be null here");
                    if (!TryParseCommand(command, out var parsedCommand))
                    {
                        _logger.Error($"Could not parse command '{command}'.");
                        continue;
                    }

                    Debug.Assert(parsedCommand != null, "Parsed command shouldn't be null here");
                    if (parsedCommand.Name == "quit" || parsedCommand.Name == "exit" || parsedCommand.Name == "q")
                    {
                        _logger.Log("Quitting backend application.");
                        _commandReader.StopReader();
                        return 0;
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
                _cpuHandler.Tick();
                Thread.Sleep(100); // 10Hz, TODO make it configurable
            }
        }

        private static bool TryParseCommand(string command, out ParsedCommand? parsedCommand)
        {
            parsedCommand = default;
            if (string.IsNullOrEmpty(command)) return false;

            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            parsedCommand = new ParsedCommand(
                Name: parts[0], 
                Arguments: parts.Length > 1 ? parts[1..] : []);
            return true;
        }

        private record ParsedCommand(string Name, string[] Arguments) { }

        private readonly ILogger _logger;
        private readonly IInput _input;
        private readonly IOutput _output;

        private readonly CpuHandler _cpuHandler;
        private readonly GlobalCommandRegistry _globalCommandRegistry;
        private readonly StateCommandRegistry _stateCommandRegistry;
        private readonly CommandReader _commandReader;
    }
}
