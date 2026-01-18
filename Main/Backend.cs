using Backend.Commands;
using System.Data;

namespace Backend
{
    // Logging is done on STDERR
    internal static class Logger
    {
        public static void Log(string message)
        {
            Console.Error.WriteLine($"[LOG] {message}");
        }

        public static void LogUsage()
        {
            Log("Usage: backend [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [-h/--help]");
        }

        public static void Error(string message)
        {
            Console.Error.WriteLine($"[ERROR] {message}");
        }
    }

    // Output is done on STDOUT
    internal static class Output
    {
        public static void Write(string message)
        {
            Console.Out.WriteLine(message);
        }
    }

    public class Backend
    {
        public static int Main(string[] args)
        {
            // Output is done on STDOUT

            var code = ParseArgs(args, out var config);
            switch (code)
            {
                case HelpExitCode:
                    Logger.LogUsage();
                    return 0;
                case InvalidArgExitCode:
                    Logger.LogUsage();
                    return InvalidArgExitCode;
            }

            var cpuHandler = new CpuHandler(config);
            Logger.Log("Backend application started.");
            while (true)
            {
                // Main execution loop
                // Listen for commands on STDIN and execute them
                if (Console.In.Peek() >= 0)
                {
                    ParseCommand(out var name, out var commandArgs);
                    if (name == "quit" || name == "exit")
                    {
                        Logger.Log("Quitting backend application.");
                        return 0;
                    }
                    else
                    {
                        cpuHandler.RunCommand(name, commandArgs);
                    }
                }
                cpuHandler.Tick();
                Thread.Sleep(100);
            }
        }

        private static int ParseArgs(string[] args, out CPU.Config config)
        {
            int memorySize = DefaultMemorySize;
            int stackSize = DefaultStackSize;
            int registerCount = DefaultRegisterCount;

            // backend [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [-h/--help]
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-m":
                    case "--memory":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out memorySize))
                        {
                            Logger.Log($"Memory size set to {memorySize}");
                            i++;
                        }
                        else
                        {
                            Logger.Error("Invalid memory size specified.");
                        }
                        break;
                    case "-s":
                    case "--stack":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out stackSize))
                        {
                            Logger.Log($"Stack size set to {stackSize}");
                            i++;
                        }
                        else
                        {
                            Logger.Error("Invalid stack size specified.");
                        }
                        break;
                    case "--registers":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out registerCount))
                        {
                            Logger.Log($"Register count set to {registerCount}");
                            i++;
                        }
                        else
                        {
                            Logger.Error("Invalid register count specified.");
                        }
                        break;
                    case "-h":
                    case "--help":
                        config = default;
                        return HelpExitCode;
                    default:
                        Logger.Error($"Unknown argument: {args[i]}");
                        config = default;
                        return InvalidArgExitCode;
                }
            }
            config = new CPU.Config(memorySize, stackSize, registerCount);
            return 0;
        }

        private static void ParseCommand(out string name, out string[] args)
        {
            name = "";
            args = [];

            var command = Console.ReadLine();
            if (command == null) return;

            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            name = parts[0];
            args = parts.Length > 1 ? parts[1..] : [];
        }

        private const int DefaultMemorySize = 256;
        private const int DefaultStackSize = 16;
        private const int DefaultRegisterCount = 4;
        private const int HelpExitCode = 1;
        private const int InvalidArgExitCode = 2;

    }
}
