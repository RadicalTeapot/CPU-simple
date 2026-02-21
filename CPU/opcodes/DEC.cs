using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.DEC, OpcodeGroupBaseCode.SingleRegisterLogicOne)]
    internal class DEC : BaseOpcode
    {
        public DEC(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            _state = state;
            SetPhases(AluOp);
        }

        private MicroPhase AluOp()
        {
            var registerValue = _state.GetRegister(_registerIdx);
            var newValue = (byte)(registerValue - 1); // Wrap around on underflow
            _state.SetRegister(_registerIdx, newValue);
            _state.SetZeroFlag(newValue == 0);
            return MicroPhase.AluOp;
        }

        private readonly byte _registerIdx;
        private readonly State _state;
    }
}
