using System.Diagnostics;
using System.Reflection;

namespace Backend.Commands
{
    internal abstract class BaseCommandRegistry<T> where T : ICommand
    {
        public BaseCommandRegistry(CommandType commandType)
        {
            RegisterCommands(commandType);
        }

        public bool TryGetCommand(string nameOrAlias, out T? command)
        {
            if (_aliasMap.TryGetValue(nameOrAlias, out var actualName))
            {
                nameOrAlias = actualName;
            }
            if (_commands.TryGetValue(nameOrAlias, out var metadata))
            {
                command = metadata.Command;
                return true;
            }
            command = default;
            return false;
        }

        private void RegisterCommands(CommandType commandType)
        {
            var commands = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t)
                    && t.GetCustomAttribute<CommandAttribute>() != null
                    && t.GetCustomAttribute<CommandAttribute>()!.CommandType == commandType
                    && t.GetConstructor([typeof(CommandContext)]) != null); // Standard constructor signature: (CommandContext)

            _commands.Clear();
            _aliasMap.Clear();
            foreach (var command in commands)
            {
                var constructor = command.GetConstructor([typeof(CommandContext)]);
                Debug.Assert(constructor != null, "Constructor should not be null here due to filtering above.");

                var attribute = command.GetCustomAttribute<CommandAttribute>();
                Debug.Assert(attribute != null, "Attribute should not be null here due to filtering above.");

                var commandCtorArgs = new CommandContext
                (
                    Name: attribute.Name,
                    Description: attribute.Description,
                    HelpText: attribute.HelpText
                );

                if (constructor.Invoke([commandCtorArgs]) is T commandInstance) // Commands are (mostly) stateless, so a single instance is sufficient
                {
                    var validStates = command.GetCustomAttributes(typeof(ValidInStateAttribute), false)
                        .Cast<ValidInStateAttribute>()
                        .SelectMany(attr => attr.ValidStates)
                        .ToArray();

                    _commands.Add(commandInstance.Name, new CommandMetadata(commandInstance, validStates, attribute.Description));

                    // Register aliases
                    if (attribute.Aliases != null)
                    {
                        foreach (var alias in attribute.Aliases)
                        {
                            _aliasMap.Add(alias, commandInstance.Name);
                        }
                    }
                }
            }
        }

        protected class CommandMetadata(T command, Type[] validStates, string description)
        {
            public T Command { get; } = command;
            public string Description { get; } = description;
            public Type[] ValidStates { get; } = validStates;
        }

        protected readonly Dictionary<string, CommandMetadata> _commands = [];
        protected readonly Dictionary<string, string> _aliasMap = [];
    }
}
