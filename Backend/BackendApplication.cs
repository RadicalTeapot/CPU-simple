using Backend.Commands.GlobalCommands;
using Backend.Commands.StateCommands;
using Backend.IO;
using System.Diagnostics;

namespace Backend
{
    internal class BackendApplication
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
                    ParseCommand(command, out var name, out var commandArgs);

                    if (name == "quit" || name == "exit" || name == "q")
                    {
                        _logger.Log("Quitting backend application.");
                        _commandReader.StopReader();
                        return 0;
                    }

                    if (_globalCommandRegistry.TryGetCommand(name, out var globalCommand))
                    {
                        Debug.Assert(globalCommand != null);
                        _cpuHandler.HandleGlobalCommand(globalCommand, commandArgs);
                    }
                    else if (_stateCommandRegistry.TryGetCommand(name, out var stateCommand))
                    {
                        Debug.Assert(stateCommand != null);
                        _cpuHandler.HandleStateCommand(stateCommand, commandArgs);
                    }
                }
                _cpuHandler.Tick();
                Thread.Sleep(100);
            }
        }

        private static void ParseCommand(string command, out string name, out string[] args)
        {
            name = string.Empty;
            args = [];

            if (string.IsNullOrEmpty(command)) return;

            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            name = parts[0];
            args = parts.Length > 1 ? parts[1..] : [];
        }

        private readonly ILogger _logger;
        private readonly IInput _input;
        private readonly IOutput _output;

        private readonly CpuHandler _cpuHandler;
        private readonly GlobalCommandRegistry _globalCommandRegistry;
        private readonly StateCommandRegistry _stateCommandRegistry;
        private readonly CommandReader _commandReader;
    }
}
