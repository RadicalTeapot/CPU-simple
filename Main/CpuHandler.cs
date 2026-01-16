using Backend.Commands;
using CPU;
using System.Text;
using System.Xml.Linq;

namespace Backend
{
    internal interface ICpuState
    {
        ICpuState RunCommand(CPU.CPU cpu, string name, string[] args);
        ICpuState Tick(CPU.CPU cpu);
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

        public ICpuState Tick(CPU.CPU cpu)
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

        public ICpuState Tick(CPU.CPU cpu)
        {
            runCommand.Execute(cpu);
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

        public ICpuState Tick(CPU.CPU cpu)
        {
            stepCommand.Execute(cpu);
            if (stepCommand.IsComplete)
            {
                Logger.Log("Step complete, transitioning to Idle state.");
                return new IdleState();
            }
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
                _currentState = _currentState.RunCommand(_cpu, name, args);
            }
        }

public void Tick() => _currentState = _currentState.Tick(_cpu);

        private ICpuState _currentState = new IdleState();
        private readonly CPU.CPU _cpu;
        private CPU.CpuInspector _inspector;
    }
}
