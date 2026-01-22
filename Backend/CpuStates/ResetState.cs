using Backend.IO;
namespace Backend.CpuStates
{
    internal class ResetState(CpuStateContext context, IOutput output)
        : ExecutingCpuState(context, output, "reset")
    {
        protected override bool IsExecutionComplete { get => true; }

        protected override void ExecuteStep()
        {
            Context.Cpu.Reset();
        }
    }
}
