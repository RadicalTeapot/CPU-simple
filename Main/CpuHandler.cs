namespace Backend
{
    internal interface ICpuState
    {
        ICpuState GetNextStateFromCommand(CommandType command, string[] args);
        ICpuState Execute();
    }

    internal class IdleState(CPU.CPU cpu) : ICpuState
    {
        public ICpuState GetNextStateFromCommand(CommandType command, string[] args)
        {
            _command = command;
            _args = args;

            return command switch
            {
                CommandType.Load => this,
                CommandType.Reset => this,
                CommandType.Pause => this,
                CommandType.Run => new RunningState(cpu, args),
                CommandType.Step => new SteppingState(cpu, args),
                _ => throw new NotImplementedException(),
            };
        }

        public ICpuState Execute()
        {
            switch (_command)
            {
                case CommandType.Load:
                    if (_args.Length == 0)
                    {
                        Logger.Error("No program specified to load.");
                        break;
                    }
                    var programPath = _args[0];
                    try
                    {
                        var programBytes = File.ReadAllBytes(programPath);
                        cpu.LoadProgram(programBytes);
                        Logger.Log($"Program loaded from {programPath}.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to load program: {ex.Message}");
                    }
                    break;
                case CommandType.Reset:
                    cpu.Reset();
                    Logger.Log("CPU reset.");
                    break;
                case CommandType.None:
                    break;
                default:
                    throw new NotImplementedException();
            }

            _command = CommandType.None;
            _args = [];

            return this;
        }

        private CommandType _command = CommandType.None;
        private string[] _args = [];
    }

    internal class RunningState(CPU.CPU cpu, string[] args) : ICpuState
    {
        public ICpuState GetNextStateFromCommand(CommandType command, string[] args)
        {
            _command = command;
            _args = args;

            return command switch
            {
                CommandType.Reset => this,
                CommandType.Run => this,
                CommandType.Step => new SteppingState(cpu, args),
                CommandType.Pause => new IdleState(cpu),
                _ => throw new NotImplementedException(),
            };
        }

        public ICpuState Execute()
        {
            switch (_command)
            {
                case CommandType.Reset:
                    cpu.Reset();
                    Logger.Log("CPU reset.");
                    break;
                case CommandType.Run:
                    _runArgs = _args;
                    Logger.Log("Run arguments updated.");
                    break;
                case CommandType.None:
                    break;
                default:
                    throw new NotImplementedException();
            }

            // TODO : Handle run arguments (e.g. breakpoints, etc.)
            cpu.Step();
            Logger.Log("CPU stepped in Running state.");

            _command = CommandType.None;
            _args = [];
            return this;
        }

        private CommandType _command = CommandType.None;
        private string[] _args = [];
        private string[] _runArgs = args;
    }

    internal class SteppingState : ICpuState
    {
        public SteppingState(CPU.CPU cpu, string[] args)
        {
            _maxSteps = (args.Length > 0 ? int.Parse(args[0]) : 1);
            _cpu = cpu;
        }
        public ICpuState GetNextStateFromCommand(CommandType command, string[] args)
        {
            _command = command;
            _args = args;

            return command switch
            {
                CommandType.Reset => this,
                CommandType.Run => new RunningState(_cpu, args),
                CommandType.Step => this,
                CommandType.Pause => new IdleState(_cpu),
                _ => throw new NotImplementedException(),
            };
        }

        public ICpuState Execute()
        {
            switch (_command)
            {
                case CommandType.Reset:
                    _cpu.Reset();
                    Logger.Log("CPU reset.");
                    break;
                case CommandType.Step:
                    if (_args.Length > 0 && int.TryParse(_args[0], out var stepCount))
                    {
                        _maxSteps = stepCount;
                        _stepsExecuted = 0;
                        Logger.Log("Step arguments updated.");
                    }
                    else
                    {
                        Logger.Error("Invalid step count argument, skipping update.");
                    }
                    break;
                case CommandType.None:
                    break;
                default:
                    throw new NotImplementedException();
            }

            // TODO : Handle step arguments (e.g. step count, etc.)
            _cpu.Step();
            Logger.Log("CPU stepped in Stepping state.");

            _stepsExecuted++;
            if (_stepsExecuted >= _maxSteps)
            {
                Logger.Log("Stepping complete, transitioning to Idle state.");
                return new IdleState(_cpu);
            }

            _command = CommandType.None;
            _args = [];
            return this;
        }

        private CommandType _command  = CommandType.None;
        private string[] _args = [];
        private int _maxSteps = 1;
        private int _stepsExecuted = 0;
        private readonly CPU.CPU _cpu;
    }

    internal class CpuHandler(CPU.CPU cpu)
    {
        public void HandleCommand(CommandType command, string[] args) => _currentState = _currentState.GetNextStateFromCommand(command, args);
        public void Execute() => _currentState = _currentState.Execute();

        private ICpuState _currentState = new IdleState(cpu);
    }
}
