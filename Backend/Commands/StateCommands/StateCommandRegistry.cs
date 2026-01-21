namespace Backend.Commands.StateCommands
{
    internal class StateCommandRegistry : BaseCommandRegistry<IStateCommand> 
    {
        public StateCommandRegistry() : base(CommandType.State) { }

        public string[] GetAvailableCommandsForState(Type cpuStateType)
        {
            return [.. _commands.Values
                .Where(metadata => metadata.ValidStates.Contains(cpuStateType))
                .Select(metadata => metadata.Command.Name)];
        }
    }
}
