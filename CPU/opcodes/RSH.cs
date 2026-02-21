using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RSH, OpcodeGroupBaseCode.BitsManipulation)]
    internal class RSH : BaseOpcode
    {
        public RSH(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.AluOp, AluOp);
        }

        public MicroPhase AluOp()
        {
            var value = _state.GetRegister(_registerIdx);
            _state.SetRegister(_registerIdx, (byte)(value >> 1));
            _state.SetCarryFlag((value & 0x01) == 0x01); // Set carry flag to the bit that was shifted out (bit 0 of the original value)
            return MicroPhase.Done;
        }

        private readonly State _state;
        private readonly byte _registerIdx;
    }
}
