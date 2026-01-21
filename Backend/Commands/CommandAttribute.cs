namespace Backend.Commands
{
    public enum CommandType
    {
        State,
        Global,
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal class CommandAttribute(CommandType commandType, string name, string[]? aliases = null, string description = "", string helpText = "") : Attribute
    {
        public CommandType CommandType { get; } = commandType;
        public string Name { get; } = name;
        public string[]? Aliases { get; } = aliases; // Null if no aliases
        public string Description { get; } = description;
        public string HelpText { get; } = helpText;
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal class ValidInStateAttribute(params Type[] validStates) : Attribute
    {
        public Type[] ValidStates { get; } = validStates;
    }
}
