using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.DEC, OpcodeGroupBaseCode.SingleRegisterLogicOne)]
    internal class DEC : BaseOpcode
    {
        public DEC(byte instructionByte, State state, IBus bus, Stack stack)
        {
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            _state = state;
            SetPhases(MicroPhase.AluOp, AluOp);
        }

        private MicroPhase AluOp()
        {
            var registerValue = _state.GetRegister(_registerIdx);
            var newValue = (byte)(registerValue - 1); // Wrap around on underflow
            _state.SetRegister(_registerIdx, newValue);
            _state.SetZeroFlag(newValue == 0);
            return MicroPhase.Done;
        }

        private readonly byte _registerIdx;
        private readonly State _state;
    }
}
