namespace Backend.CpuStates
{
    internal class IdleState(CpuStateContext context) 
        : BaseCpuState(context, "idle") 
    {
        public override ICpuState Tick()
        {
            return this;
        }
    }
}
