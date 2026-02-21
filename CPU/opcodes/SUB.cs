using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SUB, OpcodeGroupBaseCode.Subtract)]
    internal class SUB : BaseOpcode
    {
        public SUB(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _highRegisterIdx = OpcodeHelpers.GetHighRegisterIdx(instructionByte);
            _lowRegisterIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            SetPhases(AluOp);
        }

        public MicroPhase AluOp()
        {
            var firstValue = _state.GetRegister(_highRegisterIdx);
            var secondValue = _state.GetRegister(_lowRegisterIdx);
            var result = secondValue - firstValue - (1 - _state.GetCarryFlagAsInt());
            _state.SetRegister(_lowRegisterIdx, (byte)result); // Wrap around on underflow
            _state.SetCarryFlag(result >= 0); // No borrow carry
            _state.SetZeroFlag(result == 0);
            return MicroPhase.AluOp;
        }

        private readonly byte _highRegisterIdx;
        private readonly byte _lowRegisterIdx;
        private readonly State _state;
    }
}
