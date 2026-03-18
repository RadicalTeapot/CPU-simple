using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.CPI, OpcodeGroupBaseCode.SingleRegisterALU)]
    internal class CPI : BaseOpcode
    {
        public CPI(byte instructionByte, State state, IBus bus, Stack stack)
        {
            _state = state;
            _bus = bus;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.FetchOperand, ReadImmediateValue, AluOp);
        }

        private MicroPhase ReadImmediateValue()
        {
            _immediateValue = _bus.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.AluOp;
        }

        private MicroPhase AluOp()
        {
            var currentValue = _state.GetRegister(_registerIdx);
            _state.SetCarryFlag(currentValue >= _immediateValue); // Similar to SUB (no borrow), but without actual subtraction
            _state.SetZeroFlag(currentValue == _immediateValue);
            return MicroPhase.Done;
        }

        private byte _immediateValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly IBus _bus;
    }
}
