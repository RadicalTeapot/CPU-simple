using Backend.Commands.StateCommands;
using CPU;

namespace Backend.CpuStates
{
    internal class CpuStateFactory(
        CPU.CPU cpu,
        StateCommandRegistry commandRegistry)
    {
        public IdleState CreateIdleState()
        {
            return new IdleState(GetContextForState(typeof(IdleState)));
        }

        public LoadingState CreateLoadingState(byte[] program)
        {
            return new LoadingState(GetContextForState(typeof(LoadingState)), program);
        }

        public ResetState CreateResetState()
        {
            return new ResetState(GetContextForState(typeof(ResetState)));
        }

        public RunningState CreateRunningState(Run.Config runConfig)
        {
            return new RunningState(GetContextForState(typeof(RunningState)), runConfig);
        }

        public SteppingState CreateSteppingState(int numberOfInstructions)
        {
            return new SteppingState(GetContextForState(typeof(SteppingState)), numberOfInstructions);
        }

        public ErrorState CreateErrorState(string reason)
        {
            return new ErrorState(GetContextForState(typeof(ErrorState)), reason);
        }

        public HaltedState CreateHaltedState()
        {
            return new HaltedState(GetContextForState(typeof(HaltedState)));
        }

        private CpuStateContext GetContextForState(Type stateType)
        {
            return new CpuStateContext(
                StateFactory: this,
                Cpu: cpu,
                commandRegistry.GetAvailableCommandsForState(stateType));
        }
    }
}
