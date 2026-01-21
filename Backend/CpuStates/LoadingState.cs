namespace Backend.CpuStates
{
    internal class LoadingState(CpuStateContext context, byte[] program)
        : BaseCpuState(context, "loading")
    {
        public override ICpuState Tick()
        {
            Context.Cpu.LoadProgram(program);
            Context.Cpu.Reset();
            return Context.StateFactory.CreateIdleState();
        }
    }
}
