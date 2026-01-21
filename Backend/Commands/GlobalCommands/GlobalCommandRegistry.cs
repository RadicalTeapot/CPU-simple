namespace Backend.Commands.GlobalCommands
{
    internal class GlobalCommandRegistry : BaseCommandRegistry<IGlobalCommand>
    {
        public GlobalCommandRegistry() : base(CommandType.Global) { }
    }
}
