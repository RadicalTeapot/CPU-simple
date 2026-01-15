using Assembler.Analysis;
using System.Diagnostics;

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
                WriteOutputFile(config.OutputFilePath!, outputBytes);
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
        /// Outputs debug information as a Lua table to the specified output path.
        /// </summary>
        /// <param name="symbols">List of <see cref="Symbol"/></param>
        /// <param name="spanAddresses">List of <see cref="SpanAddressInfo"/></param>
        /// <param name="outputPath">Path to the output file</param>
        private static void OutputDebugInfo(IList<Symbol> symbols, IList<SpanAddressInfo> spanAddresses, string outputPath)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("return {");
            sb.AppendLine($"  version = {DebugFileVersion},");
            sb.AppendLine("  symbols = {");
            for (int i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                var comma = i < symbols.Count - 1 ? "," : "";
                sb.AppendLine($"    {{ name =  \"{symbol.Name}\", address = {symbol.Address}, kind = \"{symbol.Kind}\" }}{comma}");
            }
            sb.AppendLine("  },");
            sb.AppendLine("  spans = {");
            for (int i = 0; i < spanAddresses.Count; i++)
            {
                var spanInfo = spanAddresses[i];
                var comma = i < spanAddresses.Count - 1 ? "," : "";
                sb.AppendLine($"    {{ line = {spanInfo.Span.Line}, start_column = {spanInfo.Span.StartColumn}, end_column = {spanInfo.Span.EndColumn}, start = {spanInfo.StartAddress}, ending = {spanInfo.EndAddress} }}{comma}");
            }
            sb.AppendLine("  }");
            sb.AppendLine("}");

            var content = sb.ToString();
            File.WriteAllText(outputPath, content);
            Console.WriteLine($"Debug information written to {outputPath}.");
        }

        private const int DebugFileVersion = 1;
    }
}