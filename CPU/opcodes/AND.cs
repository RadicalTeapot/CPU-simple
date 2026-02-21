using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.AND, OpcodeGroupBaseCode.And)]
    internal class AND : BaseOpcode
    {
        public AND(byte instructionByte, State state, Memory memory, Stack stack)
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
            var value = (byte)(firstValue & secondValue);
            _state.SetRegister(_lowRegisterIdx, value);
            _state.SetZeroFlag(value == 0);
            return MicroPhase.AluOp;
        }

        private readonly byte _highRegisterIdx;
        private readonly byte _lowRegisterIdx;
        private readonly State _state;
    }
}
