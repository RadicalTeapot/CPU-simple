using Backend.IO;
namespace Backend.CpuStates
{
    internal class ResetState(CpuStateContext context, BreakpointContainer breakpointContainer, IOutput output)
        : ExecutingCpuState(context, breakpointContainer, output, "reset")
    {
        protected override bool IsExecutionComplete { get => true; }

        protected override void ExecuteStep()
        {
            Context.Cpu.Reset();
        }
    }
}
