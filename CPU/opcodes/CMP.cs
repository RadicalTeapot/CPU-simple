using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CMP, OpcodeGroupBaseCode.TwoRegistersCompare)]
    internal class CMP : BaseOpcode
    {
        public CMP(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _highRegisterIdx = OpcodeHelpers.GetHighRegisterIdx(instructionByte);
            _lowRegisterIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            SetPhases(AluOp);
        }

        public MicroPhase AluOp()
        {
            var source = _state.GetRegister(_highRegisterIdx);
            var destination = _state.GetRegister(_lowRegisterIdx);
            _state.SetZeroFlag(destination == source);
            _state.SetCarryFlag(destination >= source); // Similar to SUB (no borrow), but without actual subtraction
            return MicroPhase.AluOp;
        }

        private readonly byte _highRegisterIdx;
        private readonly byte _lowRegisterIdx;
        private readonly State _state;
    }
}
