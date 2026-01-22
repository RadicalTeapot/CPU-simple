using Backend.IO;

namespace Backend.CpuStates
{
    internal abstract class ExecutingCpuState(CpuStateContext context, IOutput output, string stateName) 
        : BaseCpuState(context, stateName)
    {
        protected abstract bool IsExecutionComplete { get; }

        protected abstract void ExecuteStep();

        public override ICpuState Tick()
        {
            ExecuteStep();

            var inspector = Context.Cpu.GetInspector();
            output.WriteStatus(inspector);
            
            if (IsExecutionComplete)
            {
                return Context.StateFactory.CreateIdleState();
            }
            return this;
        }
    }
}
