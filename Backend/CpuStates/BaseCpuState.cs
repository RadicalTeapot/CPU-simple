using Backend.Commands.StateCommands;
using CPU;

namespace Backend.CpuStates
{
    internal record CpuStateContext(
        CpuStateFactory StateFactory, CPU.CPU Cpu, string[] ValidCommands)
    { }

    internal abstract class BaseCpuState(CpuStateContext context, string stateName) : ICpuState
    {
        public ICpuState GetStateForCommand(IStateCommand command, string[] args)
        {
            if (!IsCommandAvailable(command.Name))
            {
                Logger.Error($"Command '{command.Name}' is not available in {stateName} state.");
                LogHelp();
                return this;
            }

            var result = command.GetStateForCommand(Context.StateFactory, args);
            if (!result.Success)
            {
                Logger.Error(result.Message ?? $"Command '{command.Name}' failed to execute.");
                return this;
            }

            Logger.Log(result.Message ?? $"Command '{command.Name}' executed successfully.");

            return result.NextState ?? this;
        }

        public virtual void LogHelp()
        {
            Logger.Log($"Cpu is in {stateName} state, available commands: {string.Join(',', Context.ValidCommands)}");
        }

        public abstract ICpuState Tick();

        protected CpuStateContext Context { get => context; }

        private bool IsCommandAvailable(string commandName)
        {
            return context.ValidCommands.Contains(commandName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
