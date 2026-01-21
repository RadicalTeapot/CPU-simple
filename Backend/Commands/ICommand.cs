namespace Backend.Commands
{
    internal interface ICommand
    {
        string Name { get; }
        string Description { get; }
        string HelpText { get; }
    }

    internal record CommandContext(
        string Name,
        string Description,
        string HelpText);
}
