using CPU.components;

namespace CPU.opcodes
{
    internal class ADI(State cpuState, Memory memory): BaseOpcode(OpcodeBaseCode.ADI, 2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = args[0];
            var immediateValue = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(ADI),
                Args = $"RD: {destReg}, IMM: {immediateValue}",
                RBefore = [CpuState.GetRegister(destReg)],
            };

            var currentValue = CpuState.GetRegister(destReg);
            var result = currentValue + immediateValue + CpuState.GetCarryFlagAsInt();
            CpuState.SetRegister(destReg, (byte)result); // Wrap around on overflow
            CpuState.SetCarryFlag(result > 0xFF);
            CpuState.SetZeroFlag(result == 0);
            trace.RAfter = [CpuState.GetRegister(destReg)];
            return trace;
        }
    }
}