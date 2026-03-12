using Backend.IO;
using PPU.Configuration;

namespace Backend
{
    public class Backend
    {
        public static int Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var code = ParseArgs(args, logger, out var config, out var scale, out var chrPath);
            switch (code)
            {
                case HelpExitCode:
                    logger.LogUsage();
                    return 0;
                case InvalidArgExitCode:
                    logger.LogUsage();
                    return InvalidArgExitCode;
            }

            byte[]? chrData = null;
            if (chrPath != null)
            {
                if (!File.Exists(chrPath))
                {
                    logger.Error($"CHR ROM file not found: {chrPath}");
                    return InvalidArgExitCode;
                }
                chrData = File.ReadAllBytes(chrPath);
            }

            var ppuConfig = config.VramSize > 0 ? PpuConfig.Minimal8Bit : null;
            var application = new BackendApplication(logger, new ConsoleInput(), new ConsoleOutput(), config, ppuConfig, scale, chrData);
            return application.Run();
        }

        internal static int ParseArgs(string[] args, ILogger logger, out CPU.Config config, out int scale, out string? chrPath)
        {
            var memorySize = DefaultMemorySize;
            var stackSize = DefaultStackSize;
            var registerCount = DefaultRegisterCount;
            var vramSize = DefaultVramSize;
            scale = DefaultScale;
            chrPath = null;

            // backend [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [--vram SIZE] [--scale N] [--chr PATH] [-h/--help]
            for (var i = 0; i < args.Length; i++)
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
                    case "--vram":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out vramSize))
                        {
                            logger.Log($"VRAM size set to {vramSize}");
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid VRAM size specified.");
                            config = default;
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--scale":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out scale))
                        {
                            logger.Log($"Display scale set to {scale}");
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid scale specified.");
                            config = default;
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--chr":
                        if (i + 1 < args.Length)
                        {
                            chrPath = args[++i];
                        }
                        else
                        {
                            logger.Error("No path specified for --chr.");
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
            config = new CPU.Config(memorySize, stackSize, registerCount, vramSize);
            return 0;
        }

        private const int DefaultMemorySize = 256;
        private const int DefaultStackSize = 16;
        private const int DefaultRegisterCount = 4;
        private const int DefaultVramSize = 0;
        private const int DefaultScale = 4;
        private const int HelpExitCode = 1;
        private const int InvalidArgExitCode = 2;
    }
}
