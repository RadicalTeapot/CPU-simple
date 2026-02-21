using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CPI, OpcodeGroupBaseCode.SingleRegisterALU)]
    internal class CPI : BaseOpcode
    {
        public CPI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            SetPhases(ReadImmediateValue, AluOp);
        }

        private MicroPhase ReadImmediateValue()
        {
            _immediateValue = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
        }

        private MicroPhase AluOp()
        {
            var currentValue = _state.GetRegister(_registerIdx);
            _state.SetCarryFlag(currentValue >= _immediateValue); // Similar to SUB (no borrow), but without actual subtraction
            _state.SetZeroFlag(currentValue == _immediateValue);
            return MicroPhase.AluOp;
        }

        private byte _immediateValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
