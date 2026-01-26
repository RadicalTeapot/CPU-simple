using Backend.IO;

namespace Backend.CpuStates
{
    internal class LoadingState(CpuStateContext context, IOutput output, byte[] program)
        : ExecutingCpuState(context, output, "loading")
    {
        protected override bool IsExecutionComplete { get => true; }

        protected override void ExecuteStep()
        {
            Context.Cpu.LoadProgram(program);
            Context.Cpu.Reset();
        }
    }
}
