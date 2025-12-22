using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.DEC, OpcodeGroupBaseCode.SingleRegisterLogicOne, RegisterArgsCount.One, OperandType.None)]
    internal class DEC(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var registerValue = cpuState.GetRegister(args.LowRegisterIdx);
            var newValue = (byte)(registerValue - 1);
            cpuState.SetRegister(args.LowRegisterIdx, newValue);
            cpuState.SetZeroFlag(newValue == 0);
        }
    }
}
