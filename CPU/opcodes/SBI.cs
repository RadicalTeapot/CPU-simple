using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SBI, OpcodeGroupBaseCode.SingleRegisterALU)]
    internal class SBI : BaseOpcode
    {
        public SBI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            SetPhases(ReadImmediateValue, AluOp, Done);
        }

        private MicroPhase ReadImmediateValue()
        {
            _immediateValue = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
        }

        private MicroPhase AluOp()
        {
            var registerValue = _state.GetRegister(_registerIdx);
            var result = registerValue - _immediateValue - (1 - _state.GetCarryFlagAsInt());
            _state.SetRegister(_registerIdx, (byte)result); // Wrap around on underflow
            _state.SetCarryFlag(result >= 0); // No borrow carry
            _state.SetZeroFlag(result == 0);
            return MicroPhase.AluOp;
        }

        private MicroPhase Done() => MicroPhase.Done;

        private byte _immediateValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}