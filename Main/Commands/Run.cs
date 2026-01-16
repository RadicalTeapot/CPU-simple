namespace Backend.Commands
{
    internal class Run
    {
        public const string Name = "run";
        public bool IsComplete { get; private set; }

        public Run(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "to_address")
                {
                    if (args.Length != 2)
                    {
                        throw new CommandException("The 'run to_address' command requires exactly one argument: the address.");
                    }
                    if (!int.TryParse(args[1], out var address) || address < 0)
                    {
                        throw new CommandException("The address must be a non-negative integer.");
                    }
                    _mode = Mode.ToAddress;
                    _toAddress = address;
                }
                else
                {
                    throw new CommandException($"Unknown subcommand for 'run': '{args[0]}'.");
                }
            }

            _mode = Mode.Normal;
            _toAddress = 0;
            IsComplete = false;
        }

        public void Execute(CPU.CPU cpu)
        {
            cpu.Step();
            switch (_mode)
            {
                case Mode.ToAddress:
                    // TODO Get PC from CPU and stop when it reaches ToAddress
                    // Set IsComplete to true when done
                    break;
            }
            Logger.Log("Run command executed.");
        }
        private enum Mode
        {
            Normal,
            ToAddress,
            //ToBreakpoint,
            //ToSymbol,
        }
        private readonly Mode _mode;
        private readonly int _toAddress;
    }
}
