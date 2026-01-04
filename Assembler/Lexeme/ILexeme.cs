using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler.Lexeme
{
    internal interface ILexeme
    {
        bool TryMatch(string source, int column, out string matchedText);
    }
}
