using Backend.Commands.GlobalCommands;
using Backend.Commands.StateCommands;
using Backend.CpuStates;
using Backend.IO;
using CPU;
using CPU.opcodes;

namespace Backend
{
    internal class CpuHandler
    {
        public CpuHandler(Config config, IOutput output, ILogger logger, StateCommandRegistry cpuCommandRegistry)
        {
            _logger = logger;
            _output = output;
            _cpu = new CPU.CPU(config);
            _breakpointContainer = new BreakpointContainer();
            _cpuStateFactory = new CpuStateFactory(_cpu, _logger, _output, _breakpointContainer, cpuCommandRegistry);
            _currentState = _cpuStateFactory.CreateIdleState();
        }

        public void HandleGlobalCommand(IGlobalCommand globalCommand, string[] args)
        {
            var inspector = _cpu.GetInspector();
            var context = new GlobalCommandExecutionContext(inspector, _currentState, _breakpointContainer, _output);
            
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

        private ICpuState _currentState;
        private readonly CPU.CPU _cpu;
        private readonly CpuStateFactory _cpuStateFactory;
        private readonly ILogger _logger;
        private readonly IOutput _output;
        private readonly BreakpointContainer _breakpointContainer;
    }
}
