using Backend.IO;

namespace Backend.CpuStates
{
    internal class SteppingState(
        CpuStateContext context,
        IOutput output,
        int numberOfInstructions
        ) : ExecutingCpuState(context, output, "stepping")
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
