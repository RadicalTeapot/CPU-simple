namespace Backend.CpuStates
{
    internal class ErrorState(CpuStateContext context, string reason)
        : BaseCpuState(context, "error")
    {
        public override ICpuState Tick()
        {
            return this;
        }

        public override void LogHelp()
        {
            Logger.Log($"Cpu is in error state due to: {reason}. Available commands: {string.Join(',', Context.ValidCommands)}");
        }
    }
}
