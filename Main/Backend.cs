using System.Data;

namespace Backend
{
    internal enum CommandType
    {
        None,
        Load,
        Reset,
        Run,
        Pause,
        Step,
        Quit,
        Help,
        Unknown
    }

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

            var cpu = new CPU.CPU(config);
            var cpuHandler = new CpuHandler(cpu);
            Logger.Log("Backend application started.");
            while (true)
            {
                // Main execution loop
                // Listen for commands on STDIN and execute them
                if (Console.KeyAvailable)
                {
                    ParseCommand(out var commandType, out var commandArgs);
                    if (commandType == CommandType.Quit)
                    {
                        Logger.Log("Quitting backend application.");
                        return 0;
                    }
                    else if (commandType == CommandType.Help)
                    {
                        Logger.Log("Available commands: load, reset, run, step [step_count], quit / exit, help / ?");
                    }
                    else
                    {
                        cpuHandler.HandleCommand(commandType, commandArgs);
                    }
                }
                cpuHandler.Execute();
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

        private static void ParseCommand(out CommandType type, out string[] args)
        {
            type = CommandType.Unknown;
            args = [];

            var command = Console.ReadLine();
            if (command == null) return;

            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            type = parts[0] switch
            {
                "load" => CommandType.Load,
                "reset" => CommandType.Reset,
                "run" => CommandType.Run,
                "pause" => CommandType.Pause,
                "step" => CommandType.Step,
                "quit" => CommandType.Quit,
                "exit" => CommandType.Quit,
                "help" => CommandType.Help,
                "?" => CommandType.Help,
                _ => CommandType.Unknown
            };

            args = parts.Length > 1 ? parts[1..] : [];
        }

        private const int DefaultMemorySize = 256;
        private const int DefaultStackSize = 16;
        private const int DefaultRegisterCount = 4;
        private const int HelpExitCode = 1;
        private const int InvalidArgExitCode = 2;

    }
}
