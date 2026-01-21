using CPU;

namespace Backend.CpuStates
{
    internal class SteppingState(
        CpuStateContext context,
        int numberOfInstructions
        ) : ExecutingCpuState(context, "stepping")
    {
        protected override bool IsExecutionComplete { get => _executedSteps >= numberOfInstructions; }

        protected override CpuInspector ExecuteStep()
        {
            var inspector = Context.Cpu.Step();
            _executedSteps++;
            return inspector;
        }

        private int _executedSteps = 0;
    }
}
