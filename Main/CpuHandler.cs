using Backend.Commands;
using CPU;
using CPU.opcodes;
using System.Text;
using System.Xml.Linq;

namespace Backend
{
    internal interface ICpuState
    {
        ICpuState RunCommand(CPU.CPU cpu, string name, string[] args);
        ICpuState Tick(CPU.CPU cpu, CpuInspector inspector);
    }

    internal class IdleState : ICpuState
    {
        public ICpuState RunCommand(CPU.CPU cpu, string name, string[] args)
        {
            switch (name)
            {
                case Load.Name:
                    new Load(args).Execute(cpu);
                    break;
                case Reset.Name:
                    Reset.Execute(cpu);
                    break;
                case Run.Name:
                    return new RunningState(new Run(args));
                case Step.Name:
                    return new SteppingState(new Step(args));
                case "help":
                case "?":
                    Logger.Log("Cpu is in Idle state.");
                    Logger.Log("Available commands in Idle state: load, reset, run, step, help / ?");
                    break;
                default:
                    Logger.Error($"Unsupported command '{name}' in Idle state.");
                    Logger.Log("Available commands in Idle state: load, reset, run, step, help / ?");
                    break;
            }
            return this;
        }

        public ICpuState Tick(CPU.CPU cpu, CpuInspector inspector)
        {
            return this;
        }
    }

    internal class RunningState(Run runCommand) : ICpuState
    {
        public ICpuState RunCommand(CPU.CPU cpu, string name, string[] args)
        {
            switch (name)
            {
                case Reset.Name:
                    Reset.Execute(cpu);
                    break;
                case Run.Name:
                    return new RunningState(new Run(args));
                case Step.Name:
                    return new SteppingState(new Step(args));
                case "pause":
                    Logger.Log("Pausing execution, transitioning to Idle state.");
                    return new IdleState();
                case "help":
                case "?":
                    Logger.Log("Cpu is in Run state.");
                    Logger.Log("Available commands in Run state: reset, run, step, pause, help / ?");
                    break;
                default:
                    Logger.Error($"Unsupported command '{name}' in Run state.");
                    Logger.Log("Available commands in Run state: reset, run, step, pause, help / ?");
                    break;
            }
            return this;
        }

        public ICpuState Tick(CPU.CPU cpu, CpuInspector inspector)
        {
            runCommand.Execute(cpu);
            Output.WriteStatus(inspector);
            if (runCommand.IsComplete)
            {
                Logger.Log("Run complete, transitioning to Idle state.");
                return new IdleState();
            }
            return this;
        }
    }

    internal class SteppingState(Step stepCommand) : ICpuState
    {
        public ICpuState RunCommand(CPU.CPU cpu, string name, string[] args)
        {
            switch (name)
            {
                case Reset.Name:
                    Reset.Execute(cpu);
                    break;
                case Run.Name:
                    return new RunningState(new Run(args));
                case Step.Name:
                    return new SteppingState(new Step(args));
                case "pause":
                    Logger.Log("Pausing execution, transitioning to Idle state.");
                    return new IdleState();
                case "help":
                case "?":
                    Logger.Log("Cpu is in Stepping state.");
                    Logger.Log("Available commands in Stepping state: reset, run, step, pause, help / ?");
                    break;
                default:
                    Logger.Error($"Unsupported command '{name}' in Stepping state.");
                    Logger.Log("Available commands in Stepping state: reset, run, step, pause, help / ?");
                    break;
            }
            return this;
        }

        public ICpuState Tick(CPU.CPU cpu, CpuInspector inspector)
        {
            stepCommand.Execute(cpu);
            Output.WriteStatus(inspector);
            if (stepCommand.IsComplete)
            {
                Logger.Log("Step complete, transitioning to Idle state.");
                return new IdleState();
            }
            return this;
        }
    }

    internal class HaltedState : ICpuState
    {
        public ICpuState RunCommand(CPU.CPU cpu, string name, string[] args)
        {
            switch (name)
            {
                case Load.Name:
                    new Load(args).Execute(cpu);
                    return new IdleState();
                case Reset.Name:
                    Reset.Execute(cpu);
                    return new IdleState();
                case "help":
                case "?":
                    Logger.Log("Cpu is in Halted state.");
                    Logger.Log("Available commands in Halted state: load, reset, help / ?");
                    break;
                default:
                    Logger.Error($"Unsupported command '{name}' in Halted state.");
                    Logger.Log("Available commands in Halted state: load, reset, help / ?");
                    break;
            }
            return this;
        }

        public ICpuState Tick(CPU.CPU cpu, CpuInspector inspector)
        {
            return this;
        }
    }

    internal class ErrorState : ICpuState
    {
        public ICpuState RunCommand(CPU.CPU cpu, string name, string[] args)
        {
            switch (name)
            {
                case Load.Name:
                    new Load(args).Execute(cpu);
                    return new IdleState();
                case "help":
                case "?":
                    Logger.Log("Cpu is in Error state.");
                    Logger.Log("Available commands in Error state: load, help / ?");
                    break;
                default:
                    Logger.Error($"Unsupported command '{name}' in Error state.");
                    Logger.Log("Available commands in Error state: load, help / ?");
                    break;
            }
            return this;
        }
        public ICpuState Tick(CPU.CPU cpu, CpuInspector inspector)
        {
            return this;
        }
    }

    internal class CpuHandler
    {
        public CpuHandler(CPU.Config config)
        {
            _inspector = new CPU.CpuInspector();
            _cpu = new CPU.CPU(config)
            {
                ProgressInspector = new Progress<CPU.CpuInspector>(inspector => _inspector = inspector)
            };
        }

        public void RunCommand(string name, string[] args)
        {
            if (name == Status.Name)
            {
                Status.Execute(_inspector);
            }
            else if (name == ReadMemory.Name)
            {
                var readMemCommand = new ReadMemory(args);
                readMemCommand.Execute(_inspector);
            }
            else if (name == ReadStack.Name)
            {
                ReadStack.Execute(_inspector);
            }
            else
            {
                ICpuState nextState;
                try
                {
                    nextState = _currentState.RunCommand(_cpu, name, args);
                }
                catch (OpcodeException.HaltException)
                {
                    Logger.Log("CPU reached HALT instruction.");
                    nextState = new HaltedState();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to run command: {ex.Message}");
                    nextState = new ErrorState();
                }

                _currentState = nextState;
            }
        }

        public void Tick() 
        {
            ICpuState nextState;
            try
            {
                nextState = _currentState.Tick(_cpu, _inspector);
            }
            catch (OpcodeException.HaltException)
            {
                Logger.Log("CPU reached HALT instruction.");
                nextState = new HaltedState();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to run command: {ex.Message}");
                nextState = new ErrorState();
            }
            _currentState = nextState;
        }

        private ICpuState _currentState = new IdleState();
        private readonly CPU.CPU _cpu;
        private CpuInspector _inspector;
    }
}
