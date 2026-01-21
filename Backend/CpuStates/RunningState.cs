using Backend.Commands.StateCommands;
using CPU;

namespace Backend.CpuStates
{
    internal class RunningState(
        CpuStateContext context,
        Run.Config config
        ) : ExecutingCpuState(context, "running")
    {
        protected override bool IsExecutionComplete { get => _isComplete; }

        protected override CpuInspector ExecuteStep()
        {
            var inspector = Context.Cpu.Step();
            _isComplete = config.Mode switch
            {
                Run.Mode.ToHalt => false,
                Run.Mode.ToAddress => inspector.PC == config.Address,
                _ => throw new NotImplementedException($"Run mode {config.Mode} not implemented.")
            };
            return inspector;
        }

        private bool _isComplete = false;
    }
}
