using Backend.Commands.StateCommands;
using Backend.IO;
using CPU;

namespace Backend.CpuStates
{
    internal class CpuStateFactory(
        CPU.CPU cpu,
        ILogger logger,
        IOutput output,
        BreakpointContainer breakpoints,
        WatchpointContainer watchpoints,
        StateCommandRegistry commandRegistry)
    {
        public IdleState CreateIdleState()
        {
            return new IdleState(GetContextForState(typeof(IdleState)));
        }

        public LoadingState CreateLoadingState(byte[] program)
        {
            return new LoadingState(GetContextForState(typeof(LoadingState)), breakpoints, watchpoints, output, program);
        }

        public ResetState CreateResetState()
        {
            return new ResetState(GetContextForState(typeof(ResetState)), breakpoints, watchpoints, output);
        }

        public RunningState CreateRunningState(Run.Config runConfig)
        {
            return new RunningState(GetContextForState(typeof(RunningState)), breakpoints, watchpoints, output, runConfig);
        }

        public SteppingState CreateSteppingState(int numberOfInstructions)
        {
            return new SteppingState(GetContextForState(typeof(SteppingState)), breakpoints, watchpoints, output, numberOfInstructions);
        }

        public TickingState CreateTickingState(int numberOfTicks)
        {
            return new TickingState(GetContextForState(typeof(TickingState)), breakpoints, watchpoints, output, numberOfTicks);
        }

        public ErrorState CreateErrorState(string reason)
        {
            return new ErrorState(GetContextForState(typeof(ErrorState)), reason);
        }

        public HaltedState CreateHaltedState()
        {
            return new HaltedState(GetContextForState(typeof(HaltedState)));
        }

        public CpuInspector GetInspector() => cpu.GetInspector();

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
