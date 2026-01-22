using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.IO
{
    internal interface IInput
    {
        string? ReadLine();
        ValueTask<string?> ReadLineAsync(CancellationToken ct);
    }

    internal class ConsoleInput : IInput
    {
        public string? ReadLine()
        {
            return Console.In.ReadLine();
        }

        public ValueTask<string?> ReadLineAsync(CancellationToken ct)
        {
            return Console.In.ReadLineAsync(ct);
        }
    }
}
