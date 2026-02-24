using Backend.IO;

namespace Backend.CpuStates
{
    internal class LoadingState(CpuStateContext context, BreakpointContainer breakpointContainer, WatchpointContainer watchpointContainer, IOutput output, byte[] program)
        : ExecutingCpuState(context, breakpointContainer, watchpointContainer, output, "loading")
    {
        protected override bool IsExecutionComplete { get => true; }

        protected override void ExecuteStep()
        {
            Context.Cpu.LoadProgram(program);
            Context.Cpu.Reset();
        }
    }
}
