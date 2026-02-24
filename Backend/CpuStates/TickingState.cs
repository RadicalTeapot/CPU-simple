using Backend.IO;

namespace Backend.CpuStates
{
    internal class TickingState(
        CpuStateContext context,
        BreakpointContainer breakpoints,
        WatchpointContainer watchpoints,
        IOutput output,
        int numberOfTicks
        ) : ExecutingCpuState(context, breakpoints, watchpoints, output, "ticking")
    {
        protected override bool IsExecutionComplete { get => _executedTicks >= numberOfTicks; }

        protected override void ExecuteStep()
        {
            Context.Cpu.Tick();
            _executedTicks++;
        }

        private int _executedTicks = 0;
    }
}
