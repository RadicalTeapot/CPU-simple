using Backend.IO;

namespace Backend
{
    public class Backend
    {
        public static int Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var code = ParseArgs(args, logger, out var config);
            switch (code)
            {
                case HelpExitCode:
                    logger.LogUsage();
                    return 0;
                case InvalidArgExitCode:
                    logger.LogUsage();
                    return InvalidArgExitCode;
            }

            var application = new BackendApplication(logger, new ConsoleInput(), new ConsoleOutput(), config);
            return application.Run();
        }

        internal static int ParseArgs(string[] args, ILogger logger, out CPU.Config config)
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
                            logger.Log($"Memory size set to {memorySize}");
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid memory size specified.");
                            config = default;
                            return InvalidArgExitCode;
                        }
                        break;
                    case "-s":
                    case "--stack":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out stackSize))
                        {
                            logger.Log($"Stack size set to {stackSize}");
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid stack size specified.");
                            config = default;
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--registers":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out registerCount))
                        {
                            logger.Log($"Register count set to {registerCount}");
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid register count specified.");
                            config = default;
                            return InvalidArgExitCode;
                        }
                        break;
                    case "-h":
                    case "--help":
                        config = default;
                        return HelpExitCode;
                    default:
                        logger.Error($"Unknown argument: {args[i]}");
                        config = default;
                        return InvalidArgExitCode;
                }
            }
            config = new CPU.Config(memorySize, stackSize, registerCount);
            return 0;
        }

        private const int DefaultMemorySize = 256;
        private const int DefaultStackSize = 16;
        private const int DefaultRegisterCount = 4;
        private const int HelpExitCode = 1;
        private const int InvalidArgExitCode = 2;
    }
}
