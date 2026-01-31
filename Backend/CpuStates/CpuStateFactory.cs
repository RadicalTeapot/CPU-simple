using Backend.Commands.StateCommands;
using Backend.IO;

namespace Backend.CpuStates
{
    internal class CpuStateFactory(
        CPU.CPU cpu,
        ILogger logger,
        IOutput output,
        BreakpointContainer breakpoints,
        StateCommandRegistry commandRegistry)
    {
        public IdleState CreateIdleState()
        {
            return new IdleState(GetContextForState(typeof(IdleState)));
        }

        public LoadingState CreateLoadingState(byte[] program)
        {
            return new LoadingState(GetContextForState(typeof(LoadingState)), breakpoints, output, program);
        }

        public ResetState CreateResetState()
        {
            return new ResetState(GetContextForState(typeof(ResetState)), breakpoints, output);
        }

        public RunningState CreateRunningState(Run.Config runConfig)
        {
            return new RunningState(GetContextForState(typeof(RunningState)), breakpoints, output, runConfig);
        }

        public SteppingState CreateSteppingState(int numberOfInstructions)
        {
            return new SteppingState(GetContextForState(typeof(SteppingState)), breakpoints, output, numberOfInstructions);
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
                Logger: logger,
                commandRegistry.GetAvailableCommandsForState(stateType));
        }
    }
}
