using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LRT, OpcodeGroupBaseCode.BitsManipulation)]
    internal class LRT : BaseOpcode
    {
        public LRT(byte instructionByte, State state, IBus bus, Stack stack)
        {
            _state = state;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.AluOp, AluOp);
        }

        public MicroPhase AluOp()
        {
            var value = _state.GetRegister(_registerIdx);
            var msb = (byte)(value & 0x80);
            _state.SetRegister(_registerIdx, (byte)((value << 1) | (msb >> 7)));
            return MicroPhase.Done;
        }

        private readonly State _state;
        private readonly byte _registerIdx;
    }
}
