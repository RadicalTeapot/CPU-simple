namespace Backend.Commands
{
    internal class CommandException : Exception
    {
        public CommandException(string message) : base(message)
        {
        }
    }
}
