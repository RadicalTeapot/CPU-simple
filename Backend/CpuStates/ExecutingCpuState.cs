using Backend.IO;

namespace Backend.CpuStates
{
    internal abstract class ExecutingCpuState(
        CpuStateContext context, 
        BreakpointContainer breakpointContainer, 
        IOutput output, 
        string stateName) 
        : BaseCpuState(context, stateName)
    {
        protected abstract bool IsExecutionComplete { get; }

        protected abstract void ExecuteStep();

        public override ICpuState Tick()
        {
            ExecuteStep();

            var inspector = Context.Cpu.GetInspector();
            output.WriteStatus(inspector);

            if (breakpointContainer.Contains(inspector.PC))
            {
                output.WriteBreakpointHit(inspector.PC);
                Context.Logger.Log($"Breakpoint hit at address 0x{inspector.PC:X4}. Transitioning to Idle state.");
                return Context.StateFactory.CreateIdleState();
            }

            if (IsExecutionComplete)
            {
                Context.Logger.Log("Execution complete. Transitioning to Idle state.");
                return Context.StateFactory.CreateIdleState();
            }

            return this;
        }
    }
}
