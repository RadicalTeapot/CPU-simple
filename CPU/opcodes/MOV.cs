using CPU.components;

namespace CPU.opcodes
{
    internal class MOV(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.MOV, 1, cpuState, memory, RegisterArgsCount.Two)
    {
        protected override Trace Execute(byte[] args)
        {
            var srcReg = args[0];
            var destReg = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(MOV),
                Args = $"RS: {srcReg}, RD: {destReg}",
                RBefore = [CpuState.GetRegister(destReg), CpuState.GetRegister(srcReg)],
            };

            var value = CpuState.GetRegister(srcReg);
            CpuState.SetRegister(destReg, value);

            trace.RAfter = [CpuState.GetRegister(destReg), CpuState.GetRegister(srcReg)];
            return trace;
        }
    }
}