using Emulator.IO;
namespace Emulator.CpuStates
{
    internal class ResetState(CpuStateContext context, BreakpointContainer breakpointContainer, WatchpointContainer watchpointContainer, IOutput output)
        : ExecutingCpuState(context, breakpointContainer, watchpointContainer, output, "reset")
    {
        protected override bool IsExecutionComplete { get => true; }

        protected override void ExecuteStep()
        {
            Context.Cpu.Reset();
        }
    }
}
