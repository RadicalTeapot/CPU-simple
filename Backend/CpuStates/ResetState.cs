using CPU;

namespace Backend.CpuStates
{
    internal class ResetState(CpuStateContext context)
        : ExecutingCpuState(context, "reset")
    {
        protected override bool IsExecutionComplete { get => true; }

        protected override CpuInspector ExecuteStep()
        {
            return Context.Cpu.Reset();
        }
    }
}
