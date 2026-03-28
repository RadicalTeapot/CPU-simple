using AudioChip.Configuration;
using Controller.Configuration;
using Emulator.IO;
using PPU.Configuration;
using System.Text.Json;

namespace Emulator
{
    public class Emulator
    {
        public const string Usage = "emulator [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [--vram SIZE] [--scale N] [--chr PATH] [--prog PATH] [--buttons COUNT] [--sample-rate HZ] [--buffer-size N] [-h/--help]";

        public static int Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var configFile = LoadConfigFile(logger);
            var code = ParseArgs(args, logger, out var result, configFile);

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

            var controllerConfig = result.ButtonCount.HasValue // Check for negative button count is done in ValidateArgs, so we can assume it's valid if it has a value
                ? new ControllerConfiguration(result.ButtonCount.Value)
                : null;

            AudioConfiguration? audioConfig = null;
            if (ppuConfig != null)
            {
                var cpuClockRate = ppuConfig.TotalScanlines * ppuConfig.CyclesPerScanline / ppuConfig.PpuCyclesPerCpuCycle * 60;
                audioConfig = new AudioConfiguration(
                    result.SampleRate ?? DefaultSampleRate,
                    result.AudioBufferSize ?? DefaultAudioBufferSize,
                    cpuClockRate
                );
            }

            var peripherals = ppuConfig != null
                ? new PeripheralSet(ppuConfig, chrData, controllerConfig, audioConfig)
                : null;

            var context = new EmulatorApplication.EmulatorContext(
                logger,
                new ConsoleInput(),
                new ConsoleOutput(),
                result.Config,
                peripherals,
                result.Scale,
                progData
            );

            var application = new EmulatorApplication(context);
            return application.Run();
        }

        internal static int ParseArgs(string[] args, ILogger logger, out ParsedArgsResult result, EmulatorConfig? configFile = null)
        {
            int? memorySize = null;
            int? stackSize = null;
            int? registerCount = null;
            int? vramSize = null;
            int? scale = null;
            string? chrPath = null;
            string? progPath = null;
            int? buttonCount = null;
            int? sampleRate = null;
            int? audioBufferSize = null;
            result = new(default, DefaultScale, null, null);

            // emulator [-m/--memory SIZE] [-s/--stack SIZE] [--registers COUNT] [--vram SIZE] [--scale N] [--chr PATH] [--prog PATH] [--buttons COUNT] [-h/--help]
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-m":
                    case "--memory":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var mem))
                        {
                            logger.Log($"Memory size set to {mem}");
                            memorySize = mem;
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
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var stack))
                        {
                            logger.Log($"Stack size set to {stack}");
                            stackSize = stack;
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid stack size specified.");
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--registers":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var regs))
                        {
                            logger.Log($"Register count set to {regs}");
                            registerCount = regs;
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid register count specified.");
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--vram":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var vram))
                        {
                            logger.Log($"VRAM size set to {vram}");
                            vramSize = vram;
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid VRAM size specified.");
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--scale":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var s))
                        {
                            logger.Log($"Display scale set to {s}");
                            scale = s;
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
                    case "--buttons":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var buttons))
                        {
                            logger.Log($"Button count set to {buttons}");
                            buttonCount = buttons;
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid button count specified.");
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--sample-rate":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var sr))
                        {
                            logger.Log($"Audio sample rate set to {sr}");
                            sampleRate = sr;
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid sample rate specified.");
                            return InvalidArgExitCode;
                        }
                        break;
                    case "--buffer-size":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var bs))
                        {
                            logger.Log($"Audio buffer size set to {bs}");
                            audioBufferSize = bs;
                            i++;
                        }
                        else
                        {
                            logger.Error("Invalid buffer size specified.");
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
                new CPU.Config(
                    memorySize ?? configFile?.Memory ?? DefaultMemorySize,
                    stackSize ?? configFile?.Stack ?? DefaultStackSize,
                    registerCount ?? configFile?.Registers ?? DefaultRegisterCount,
                    vramSize ?? configFile?.Vram ?? DefaultVramSize
                ),
                scale ?? configFile?.Scale ?? DefaultScale,
                chrPath ?? configFile?.Chr,
                progPath ?? configFile?.Prog,
                buttonCount ?? configFile?.Buttons,
                sampleRate ?? configFile?.SampleRate,
                audioBufferSize ?? configFile?.AudioBufferSize
            );
            return 0;
        }

        internal record ParsedArgsResult(
            CPU.Config Config,
            int Scale,
            string? ChrPath,
            string? ProgPath,
            int? ButtonCount = null,
            int? SampleRate = null,
            int? AudioBufferSize = null
        );

        internal record EmulatorConfig(
            int? Memory = null,
            int? Stack = null,
            int? Registers = null,
            int? Vram = null,
            int? Scale = null,
            string? Chr = null,
            string? Prog = null,
            int? Buttons = null,
            int? SampleRate = null,
            int? AudioBufferSize = null
        );

        internal static bool ValidateArgs(ParsedArgsResult args, ILogger logger)
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
            if (args.ChrPath != null && !File.Exists(args.ChrPath))
            {
                logger.Error($"Character file not found: {args.ChrPath}");
                return false;
            }
            if (args.ProgPath != null && !File.Exists(args.ProgPath))
            {
                logger.Error($"Program file not found: {args.ProgPath}");
                return false;
            }
            if (args.ButtonCount < 0)
            {
                logger.Error("Button count cannot be negative.");
                return false;
            }
            if (args.ButtonCount >= 0 && args.Config.VramSize <= 0)
            {
                logger.Error("Controller (--buttons) requires PPU (--vram must also be set).");
                return false;
            }
            if (args.SampleRate.HasValue && args.Config.VramSize <= 0)
            {
                logger.Error("Audio (--sample-rate) requires PPU (--vram must also be set).");
                return false;
            }
            if (args.AudioBufferSize.HasValue && args.Config.VramSize <= 0)
            {
                logger.Error("Audio (--buffer-size) requires PPU (--vram must also be set).");
                return false;
            }
            if (args.SampleRate.HasValue && args.SampleRate.Value <= 0)
            {
                logger.Error("Sample rate must be a positive integer.");
                return false;
            }
            if (args.AudioBufferSize.HasValue && args.AudioBufferSize.Value <= 0)
            {
                logger.Error("Buffer size must be a positive integer.");
                return false;
            }
            return true;
        }

        private static EmulatorConfig? LoadConfigFile(ILogger logger)
        {
            const string ConfigFileName = "emulator.json";
            if (!File.Exists(ConfigFileName)) return null;
            try
            {
                var json = File.ReadAllText(ConfigFileName);
                return JsonSerializer.Deserialize<EmulatorConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load config file '{ConfigFileName}': {ex.Message}");
                return null;
            }
        }

        private const int DefaultMemorySize = 256;
        private const int DefaultStackSize = 16;
        private const int DefaultRegisterCount = 4;
        private const int DefaultVramSize = 0;
        private const int DefaultScale = 4;
        private const int DefaultSampleRate = 44100;
        private const int DefaultAudioBufferSize = 1024;
        private const int HelpExitCode = 1;
        private const int InvalidArgExitCode = 2;
    }
}
