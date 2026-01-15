using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler.Analysis
{
    public enum SymbolKind
    {
        Variable,
        Function
    }

    public record Symbol(string Name, int Address, SymbolKind Kind) { }
}
