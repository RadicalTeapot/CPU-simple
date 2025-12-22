using CPU.components;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.MOV, OpcodeGroupBaseCode.Move, RegisterArgsCount.Two, OperandType.None)]
    internal class MOV(State cpuState, Memory memory, Stack stack, OpcodeArgs args) : IOpcode
    {
        public void Execute()
        {
            var value = cpuState.GetRegister(args.HighRegisterIdx);
            cpuState.SetRegister(args.LowRegisterIdx, value);
        }
    }
}