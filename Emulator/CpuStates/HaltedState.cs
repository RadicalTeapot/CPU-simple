namespace Emulator.CpuStates
{
    internal class HaltedState(CpuStateContext context)
        : BaseCpuState(context, "halted")
    {
        public override ICpuState Tick()
        {
            return this;
        }
    }
}
