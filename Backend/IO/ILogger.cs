namespace Backend.IO
{
    public interface ILogger
    {
        void Log(string message);
        void LogUsage();
        void Error(string message);
    }

    // Logging is done on STDERR
    internal class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.Error.WriteLine($"{message}");
        }

        public void LogUsage()
        {
            Log("Usage: backend [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [-h/--help]");
        }

        public void Error(string message)
        {
            Console.Error.WriteLine($"[ERROR] {message}");
        }
    }
}
