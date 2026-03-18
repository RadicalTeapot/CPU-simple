using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.SUB, OpcodeGroupBaseCode.Subtract)]
    internal class SUB : BaseOpcode
    {
        public SUB(byte instructionByte, State state, IBus bus, Stack stack)
        {
            _state = state;
            _sourceRegisterIdx = OpcodeHelpers.GetSourceRegisterIdx(instructionByte);
            _destinationRegisterIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.AluOp, AluOp);
        }

        public MicroPhase AluOp()
        {
            var firstValue = _state.GetRegister(_sourceRegisterIdx);
            var secondValue = _state.GetRegister(_destinationRegisterIdx);
            var result = secondValue - firstValue - (1 - _state.GetCarryFlagAsInt());
            _state.SetRegister(_destinationRegisterIdx, (byte)result); // Wrap around on underflow
            _state.SetCarryFlag(result >= 0); // No borrow carry
            _state.SetZeroFlag(result == 0);
            return MicroPhase.Done;
        }

        private readonly byte _sourceRegisterIdx;
        private readonly byte _destinationRegisterIdx;
        private readonly State _state;
    }
}
