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
            _sourceRegisterIdx = OpcodeHelpers.GetSourceRegisterIdx(instructionByte);
            _destinationRegisterIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.AluOp, AluOp);
        }

        public MicroPhase AluOp()
        {
            var firstValue = _state.GetRegister(_sourceRegisterIdx);
            var secondValue = _state.GetRegister(_destinationRegisterIdx);
            var value = (byte)(firstValue & secondValue);
            _state.SetRegister(_destinationRegisterIdx, value);
            _state.SetZeroFlag(value == 0);
            return MicroPhase.Done;
        }

        private readonly byte _sourceRegisterIdx;
        private readonly byte _destinationRegisterIdx;
        private readonly State _state;
    }
}
