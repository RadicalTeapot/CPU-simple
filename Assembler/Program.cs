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
                Console.WriteLine("Usage: Assembler [source_file] [-o output_file]");
                return;
            }

            string sourceCode = config.UseStdin ? ReadFromStdin() : ReadSourceFile(config.SourceFilePath!);

            byte[] outputBytes;
            try
            {
                outputBytes = ProcessSourceCode(sourceCode);
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

        private readonly struct Config
        {
            public readonly bool UseStdin;
            public readonly string? SourceFilePath;
            public readonly bool UseStdout;
            public readonly string? OutputFilePath;

            public Config(string? sourceFilePath, string? outputFilePath)
            {
                UseStdin = sourceFilePath == null;
                SourceFilePath = sourceFilePath;
                UseStdout = outputFilePath == null;
                OutputFilePath = outputFilePath;
            }
        }

        private static Config ParseArgs(string[] args)
        {
            if (args.Length > 4)
            {
                throw new ArgumentException("Too many arguments provided.");
            }

            string? sourceFilePath = null;
            string? outputFilePath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length)
                {
                    outputFilePath = args[i + 1];
                    i++;
                }
                else
                {
                    sourceFilePath = args[i];
                }
            }
            return new Config(sourceFilePath, outputFilePath);
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

        private static byte[] ProcessSourceCode(string sourceCode)
        {
            var tokens = new Lexer().Tokenize(sourceCode);
            var programNode = Parser.ParseProgram(tokens);
            var emitNodes = new Analyser().Run(programNode);
            var outputBytes = new Emitter().Emit(emitNodes);
            return outputBytes;
        }
    }
}