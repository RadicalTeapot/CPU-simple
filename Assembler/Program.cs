using Assembler.Analysis;
using System.Diagnostics;
using System.Text.Json;

namespace Assembler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Config config;
            try
            {
                config = ParseArgs(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing arguments: {ex.Message}");
                Console.WriteLine("Usage: Assembler [source_file] [-o output_file] [-d debug_file]");
                return;
            }

            string sourceCode = config.UseStdin ? ReadFromStdin() : ReadSourceFile(config.SourceFilePath!);

            byte[] outputBytes;
            try
            {
                outputBytes = ProcessSourceCode(sourceCode, config.EmitDebugFile, config.DebugFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during assembly: {ex.Message}");
                return;
            }

            if (!config.UseStdout)
            {
                Debug.Assert(config.OutputFilePath != null, "Output file path should not be null when not using stdout.");
                WriteOutputFile(config.OutputFilePath, outputBytes);
            }
            else
            {
                WriteToStdout(outputBytes);
            }
        }

        private readonly struct Config(string? sourceFilePath, string? outputFilePath, string? debugFilePath)
        {
            public readonly bool UseStdin => SourceFilePath == null;
            public readonly string? SourceFilePath = sourceFilePath;
            public readonly bool UseStdout => OutputFilePath == null;
            public readonly string? OutputFilePath = outputFilePath;
            public readonly bool EmitDebugFile => DebugFilePath != null;
            public readonly string? DebugFilePath = debugFilePath;
        }

        private static Config ParseArgs(string[] args)
        {
            if (args.Length > 6)
            {
                throw new ArgumentException("Too many arguments provided.");
            }

            string? sourceFilePath = null;
            string? outputFilePath = null;
            string? debugFilePath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length)
                {
                    outputFilePath = args[i + 1];
                    i++;
                }
                else if (args[i] == "-d" && i + 1 < args.Length)
                {
                    debugFilePath = args[i + 1];
                    i++;
                }
                else
                {
                    sourceFilePath = args[i];
                }
            }
            return new Config(sourceFilePath, outputFilePath, debugFilePath);
        }

        private static string ReadSourceFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            string sourceCode = reader.ReadToEnd();
            Console.WriteLine("Source file read successfully.");
            return sourceCode;
        }

        private static void WriteOutputFile(string filePath, byte[] data)
        {
            using var writer = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            writer.Write(data, 0, data.Length);
            Console.WriteLine($"Output written to {filePath}.");
        }

        private static string ReadFromStdin()
        {
            using var reader = new StreamReader(Console.OpenStandardInput());
            string sourceCode = reader.ReadToEnd();
            Console.WriteLine("Source code read from standard input.");
            return sourceCode;
        }

        private static void WriteToStdout(byte[] data)
        {
            using var writer = new BinaryWriter(Console.OpenStandardOutput());
            writer.Write(data);
            Console.WriteLine("Output written to standard output.");
        }

        private static byte[] ProcessSourceCode(string sourceCode, bool emitDebugFile, string? debugFilePath)
        {
            Debug.Assert(sourceCode != null, "Source code should not be null.");

            var tokens = new Lexer().Tokenize(sourceCode);
            var programNode = Parser.ParseProgram(tokens);
            var analyzer = new Analyser();
            var emitNodes = analyzer.Run(programNode);
            var emitter = new Emitter();
            var outputBytes = emitter.Emit(emitNodes);

            if (emitDebugFile)
            {
                Debug.Assert(debugFilePath != null, "Debug file path should not be null when emitting debug file.");
                var symbols = analyzer.GetSymbols();
                var spanAddresses = emitter.GetSpanAddresses();
                OutputDebugInfo(symbols, spanAddresses, debugFilePath);
            }

            return outputBytes;
        }

        /// <summary>
        /// Outputs debug information as JSON to the specified output path.
        /// </summary>
        /// <param name="symbols">List of <see cref="Symbol"/></param>
        /// <param name="spanAddresses">List of <see cref="SpanAddressInfo"/></param>
        /// <param name="outputPath">Path to the output file</param>
        private static void OutputDebugInfo(IList<Symbol> symbols, IList<SpanAddressInfo> spanAddresses, string outputPath)
        {
            var debugInfo = new
            {
                version = DebugFileVersion,
                symbols = symbols.Select(s => new
                {
                    name = s.Name,
                    address = s.Address,
                    kind = s.Kind
                }).ToArray(),
                spans = spanAddresses.Select(sa => new
                {
                    line = sa.Span.Line,
                    start_column = sa.Span.StartColumn,
                    end_column = sa.Span.EndColumn,
                    start_address = sa.StartAddress,
                    end_address = sa.EndAddress
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(debugInfo, _jsonSerializerOptions);

            File.WriteAllText(outputPath, json);
            Console.WriteLine($"Debug information written to {outputPath}.");
        }

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
        private const int DebugFileVersion = 1;
    }
}