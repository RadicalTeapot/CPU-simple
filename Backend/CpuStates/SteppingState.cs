using Backend.IO;

namespace Backend.CpuStates
{
    internal class SteppingState(
        CpuStateContext context,
        BreakpointContainer breakpoints,
        IOutput output,
        int numberOfInstructions
        ) : ExecutingCpuState(context, breakpoints, output, "stepping")
    {
        protected override bool IsExecutionComplete { get => _executedSteps >= numberOfInstructions; }

        protected override void ExecuteStep()
        {
            Context.Cpu.Step();
            _executedSteps++;
        }

        private int _executedSteps = 0;
    }
}
