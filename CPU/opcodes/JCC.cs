using CPU.components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPU.opcodes
{
    internal class JCC(State cpuState, Memory memory) : IOpcode
    {
        public void RegisterOpcode(Dictionary<OpcodeBaseCode, IOpcode> opcodeRegistry)
            => opcodeRegistry[OpcodeBaseCode.JCC] = this;

        public void Execute(out Trace trace)
        {
            var pcBefore = cpuState.GetPC();

            cpuState.IncrementPC(); // Move to operand
            var targetAddress = memory.ReadAddress(cpuState.GetPC(), out var size);

            if (cpuState.C)
                cpuState.IncrementPC(size);     // If condition not met, skip the jump address
            else
                cpuState.SetPC(targetAddress);  // Otherwise, perform the jump

            trace = new Trace()
            {
                InstructionName = nameof(JMP),
                Args = $"ADDR: {targetAddress}",
                PcBefore = pcBefore,
                PcAfter = cpuState.GetPC(),
            };
        }
    }
}
