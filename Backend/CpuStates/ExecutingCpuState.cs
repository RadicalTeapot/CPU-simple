using Backend.IO;
using CPU;

namespace Backend.CpuStates
{
    internal abstract class ExecutingCpuState(CpuStateContext context, string stateName) 
        : BaseCpuState(context, stateName)
    {
        protected abstract bool IsExecutionComplete { get; }

        protected abstract CpuInspector ExecuteStep();

        public override ICpuState Tick()
        {
            var result = ExecuteStep();
            new Output().WriteStatus(result);
            if (IsExecutionComplete)
            {
                return Context.StateFactory.CreateIdleState();
            }
            return this;
        }
    }
}
