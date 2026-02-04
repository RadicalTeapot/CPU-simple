using Backend.Commands.StateCommands;
using Backend.IO;

namespace Backend.CpuStates
{
    internal record CpuStateContext(
        CpuStateFactory StateFactory, CPU.CPU Cpu, ILogger Logger, string[] ValidCommands)
    { }

    internal abstract class BaseCpuState(CpuStateContext context, string stateName) : ICpuState
    {
        public ICpuState GetStateForCommand(IStateCommand command, string[] args)
        {
            if (!IsCommandAvailable(command.Name))
            {
                Context.Logger.Error($"Command '{command.Name}' is not available in {stateName} state.");
                LogHelp();
                return this;
            }

            var result = command.Execute(Context.StateFactory, args);
            if (!result.Success)
            {
                Context.Logger.Error(result.Message ?? $"Command '{command.Name}' failed to execute.");
                return this;
            }

            Context.Logger.Log(result.Message ?? $"Command '{command.Name}' executed successfully.");

            return result.NextState ?? this;
        }

        public virtual void LogHelp()
        {
            Context.Logger.Log($"Cpu is in {stateName} state, available commands: {string.Join(',', Context.ValidCommands)}");
        }

        public abstract ICpuState Tick();

        protected CpuStateContext Context { get => context; }

        private bool IsCommandAvailable(string commandName)
        {
            return context.ValidCommands.Contains(commandName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
