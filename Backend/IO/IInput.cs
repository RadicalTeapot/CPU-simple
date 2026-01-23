namespace Backend.IO
{
    public interface IInput
    {
        string? ReadLine();
        Task<string?> ReadLineAsync();
    }

    internal class ConsoleInput : IInput
    {
        public string? ReadLine() => Console.In.ReadLine();

        public Task<string?> ReadLineAsync() => Console.In.ReadLineAsync();
    }
}
