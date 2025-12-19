using CPU.components;

namespace CPU.opcodes
{
    internal class SBI(State cpuState, Memory memory) : BaseOpcode(OpcodeBaseCode.SBI, 2, cpuState, memory, RegisterArgsCount.One)
    {
        protected override Trace Execute(byte[] args)
        {
            var destReg = args[0];
            var immediateValue = args[1];

            var trace = new Trace()
            {
                InstructionName = nameof(SBI),
                Args = $"RD: {destReg}, IMM: {immediateValue}",
                RBefore = [CpuState.GetRegister(destReg)],
            };

            var currentValue = CpuState.GetRegister(destReg);
            var result = currentValue - immediateValue - (1 - CpuState.GetCarryFlagAsInt());
            CpuState.SetRegister(destReg, (byte)result); // Wrap around on underflow
            CpuState.SetCarryFlag(result >= 0); // No borrow carry
            CpuState.SetZeroFlag(result == 0);
            trace.RAfter = [CpuState.GetRegister(destReg)];
            return trace;
        }
    }
}