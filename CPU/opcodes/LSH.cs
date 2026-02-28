using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LSH, OpcodeGroupBaseCode.BitsManipulation)]
    internal class LSH : BaseOpcode
    {
        public LSH(byte instructionByte, State state, IBus bus, Stack stack)
        {
            _state = state;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.AluOp, AluOp);
        }

        public MicroPhase AluOp()
        {
            var value = _state.GetRegister(_registerIdx);
            _state.SetRegister(_registerIdx, (byte)(value << 1));
            _state.SetCarryFlag((value & 0x80) == 0x80); // Set carry flag to the bit that was shifted out (bit 7 of the original value)
            return MicroPhase.Done;
        }

        private readonly State _state;
        private readonly byte _registerIdx;
    }
}
