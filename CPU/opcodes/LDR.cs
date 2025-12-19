using CPU.components;

namespace CPU.opcodes
{
    internal class LDR(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.LDR, 2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = args[0];
            var memoryAddress = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(LDR),
                Args = $"RD: {destReg}, ADDR: {memoryAddress}",
                RBefore = [CpuState.GetRegister(destReg)],
            };

            CpuState.SetRegister(destReg, Memory.ReadByte(memoryAddress));
            trace.RAfter = [CpuState.GetRegister(destReg)];
            return trace;
        }
    }
}