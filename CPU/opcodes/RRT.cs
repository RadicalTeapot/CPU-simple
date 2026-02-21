using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.RRT, OpcodeGroupBaseCode.BitsManipulation)]
    internal class RRT : BaseOpcode
    {
        public RRT(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            SetPhases(AluOp);
        }

        public MicroPhase AluOp()
        {
            var value = _state.GetRegister(_registerIdx);
            var lsb = (byte)(value & 0x01);
            _state.SetRegister(_registerIdx, (byte)((value >> 1) | (lsb << 7)));
            return MicroPhase.Done;
        }

        private readonly State _state;
        private readonly byte _registerIdx;
    }
}
