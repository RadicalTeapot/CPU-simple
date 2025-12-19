using CPU.components;

namespace CPU.opcodes
{
    internal class STR(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.STR, 2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var srcReg = args[0];
            var memoryAddress = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(STR),
                Args = $"RS: {srcReg}, Memory address: {memoryAddress}",
                RBefore = [CpuState.GetRegister(srcReg)],
            };

            Memory.WriteByte(memoryAddress, CpuState.GetRegister(srcReg));
            trace.RAfter = [CpuState.GetRegister(srcReg)];
            return trace;
        }
    }
}