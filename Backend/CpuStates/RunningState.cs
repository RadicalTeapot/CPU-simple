using Backend.Commands.StateCommands;
using Backend.IO;

namespace Backend.CpuStates
{
    internal class RunningState(
        CpuStateContext context,
        BreakpointContainer breakpointContainer,
        IOutput output,
        Run.Config config
        ) : ExecutingCpuState(context, breakpointContainer, output, "running")
    {
        protected override bool IsExecutionComplete { get => _isComplete; }

        protected override void ExecuteStep()
        {
            Context.Cpu.Step();
            var inspector = Context.Cpu.GetInspector();
            _isComplete = config.Mode switch
            {
                Run.Mode.ToHalt => false,
                Run.Mode.ToAddress => inspector.PC == config.Address,
                _ => throw new NotImplementedException($"Run mode {config.Mode} not implemented.")
            };
        }

        private bool _isComplete = false;
    }
}
