using Emulator.IO;
using PPU.Configuration;

namespace Emulator
{
    public class Emulator
    {
        public const string Usage = "emulator [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [--vram SIZE] [--scale N] [--chr PATH] [--prog PATH] [-h/--help]";

        public static int Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var code = ParseArgs(args, logger, out var result);
            
            switch (code)
            {
                case HelpExitCode:
                    logger.LogUsage();
                    return 0;
                case InvalidArgExitCode:
                    logger.LogUsage();
                    return InvalidArgExitCode;
            }

            if (!ValidateArgs(result, logger))
            {
                return InvalidArgExitCode;
            }

            byte[]? chrData = null;
            if (result.ChrPath != null)
            {
                if (!File.Exists(result.ChrPath))
                {
                    logger.Error($"CHR ROM file not found: {result.ChrPath}");
                    return InvalidArgExitCode;
                }
                chrData = File.ReadAllBytes(result.ChrPath);
            }

            byte[]? progData = null;
            if (result.ProgPath != null) 
            {
                if (!File.Exists(result.ProgPath))
                {
                    logger.Error($"Program file not found: {result.ProgPath}");
                    return InvalidArgExitCode;
                }
                progData = File.ReadAllBytes(result.ProgPath);
                if (progData.Length > result.Config.MemorySize - result.Config.StackSize) // TODO This should take into account the actual memory layout and reserved areas, not just stack size (e.g. MMIO size)
                {
                    logger.Error($"Program size ({progData.Length} bytes) exceeds available memory ({result.Config.MemorySize - result.Config.StackSize} bytes).");
                    return InvalidArgExitCode;
                }
            }

            var ppuConfig = result.Config.VramSize > 0 ? PpuConfig.Minimal8Bit : null;
            if (ppuConfig != null && ppuConfig.ChrInRom && chrData == null)
            {
                logger.Error("PPU configuration requires CHR data, but no CHR ROM was provided.");
                return InvalidArgExitCode;
            }

            var context = new EmulatorApplication.EmulatorContext(
                logger,
                new ConsoleInput(),
                new ConsoleOutput(),
                result.Config,
                ppuConfig,
                result.Scale,
                chrData,
                progData
            );

            var application = new EmulatorApplication(context);
            return application.Run();
        }

        internal static int ParseArgs(string[] args, ILogger logger, out ParsedArgsResult result)
        {
            var memorySize = DefaultMemorySize;
            var stackSize = DefaultStackSize;
            var registerCount = DefaultRegisterCount;
            var vramSize = DefaultVramSize;
            var scale = DefaultScale;
            string? chrPath = null;
            string? progPath = null;
            result = new(default, scale, chrPath, progPath);

            // emulator [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [--vram SIZE] [--scale N] [--chr PATH] [--prog PATH] [-h/--help]
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
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--prog":
                        if (i + 1 < args.Length)
                        {
                            progPath = args[++i];
                        }
                        else
                        {
                            logger.Error("No path specified for --prog.");
                            return InvalidArgExitCode;
                        }
                        break;
                    case "-h":
                    case "--help":
                        return HelpExitCode;
                    default:
                        logger.Error($"Unknown argument: {args[i]}");
                        return InvalidArgExitCode;
                }
            }

            result = new(
                new CPU.Config(memorySize, stackSize, registerCount, vramSize),
                scale,
                chrPath,
                progPath
            );
            return 0;
        }

        internal record ParsedArgsResult(
            CPU.Config Config,
            int Scale,
            string? ChrPath,
            string? ProgPath
        );

        private static bool ValidateArgs(ParsedArgsResult args, ILogger logger)
        {
            if (args.Config.MemorySize <= 0)
            {
                logger.Error("Memory size must be a positive integer.");
                return false;
            }
#if x16
            if (args.Config.MemorySize > 65536)
            {
                logger.Error("Memory size exceeds maximum size (65536).");
#else
            if (args.Config.MemorySize > 256)
            {
                logger.Error("Memory size exceeds maximum size (256).");
#endif
                return false;
            }
            if (args.Config.StackSize <= 0)
            {
                logger.Error("Stack size must be a positive integer.");
                return false;
            }
            if (args.Config.StackSize >= args.Config.MemorySize)
            {
                logger.Error("Stack size must be smaller than memory size.");
                return false;
            }
            if (args.Config.RegisterCount <= 0)
            {
                logger.Error("Register count must be a positive integer.");
                return false;
            }
            if (args.Config.VramSize < 0)
            {
                logger.Error("VRAM size cannot be negative.");
                return false;
            }
            if (args.Scale <= 0)
            {
                logger.Error("Scale must be a positive integer.");
                return false;
            }
            if (args.ChrPath !=  null && !File.Exists(args.ChrPath))
            {
                logger.Error($"Character file not found: {args.ChrPath}");
                return false;
            }
            if (args.ProgPath != null && !File.Exists(args.ProgPath))
            {
                logger.Error($"Program file not found: {args.ProgPath}");
                return false;
            }
            return true;
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
