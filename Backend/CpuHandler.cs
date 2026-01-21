using Backend.Commands.GlobalCommands;
using Backend.Commands.StateCommands;
using Backend.CpuStates;
using CPU;
using CPU.opcodes;

namespace Backend
{
    internal class CpuHandler
    {
        public CpuHandler(Config config, StateCommandRegistry cpuCommandRegistry)
        {
            _inspector = new CpuInspector();
            _cpu = new CPU.CPU(config);
            _cpuStateFactory = new CpuStateFactory(_cpu, cpuCommandRegistry);
            _currentState = _cpuStateFactory.CreateIdleState();
        }

        public void HandleGlobalCommand(IGlobalCommand globalCommand, string[] args)
        {
            globalCommand.Execute(_inspector, _currentState, args);
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
                Logger.Log("CPU reached HALT instruction.");
                nextState = _cpuStateFactory.CreateHaltedState();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failure during CPU tick: {ex.Message}");
                nextState = _cpuStateFactory.CreateErrorState(ex.Message);
            }
            _currentState = nextState;
        }

        private ICpuState _currentState;
        private CpuInspector _inspector;
        private readonly CPU.CPU _cpu;
        private readonly CpuStateFactory _cpuStateFactory;
    }
}
