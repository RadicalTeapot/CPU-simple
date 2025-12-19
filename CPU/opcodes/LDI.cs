using CPU.components;

namespace CPU.opcodes
{
    internal class LDI(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.LDI, 2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = args[0];
            var immediateValue = args[1];
            
            var trace = new Trace()
            {
                InstructionName = nameof(LDI),
                Args = $"RD: {destReg}, IMM: {immediateValue}",
                RBefore = [CpuState.GetRegister(destReg)],
            };

            CpuState.SetRegister(destReg, immediateValue);
            trace.RAfter = [CpuState.GetRegister(destReg)];
            return trace;
        }
    }
}