using System.Diagnostics;
using System.Security.Cryptography;

namespace Backend.Commands
{
    internal class Load
    {
        public const string Name = "load";

        public Load(string[] args)
        {
            if (args.Length != 1)
            {
                throw new CommandException("The 'load' command requires exactly one argument: the file path.");
            }

            try
            {
                _programBytes = File.ReadAllBytes(args[0]);
            }
            catch (Exception ex)
            {
                throw new CommandException($"Failed to read program file: {ex.Message}");
            }
        }

        public void Execute(CPU.CPU cpu)
        {
            cpu.LoadProgram(_programBytes);
            Logger.Log($"Program loaded.");
        }

        private readonly byte[] _programBytes;
    }
}
