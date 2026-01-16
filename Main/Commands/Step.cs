using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Commands
{
    internal class Step
    {
        public const string Name = "step";

        public bool IsComplete { get => _stepsRemaining <= 0; }

        public Step(string[] args)
        {
            if (args.Length > 1)
            {
                throw new CommandException("The 'step' command does takes at most 1 argument.");
            }
            _stepsRemaining = args.Length == 1 && int.TryParse(args[0], out var steps) && steps > 0 ? steps : 1;
        }

        public void Execute(CPU.CPU cpu)
        {
            cpu.Step();
            _stepsRemaining--;

            Logger.Log("Stepped one instruction.");
        }

        private int _stepsRemaining = 1;
    }
}
