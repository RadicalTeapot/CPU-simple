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
            _sourceRegisterIdx = OpcodeHelpers.GetSourceRegisterIdx(instructionByte);
            _destinationRegisterIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.AluOp, AluOp);
        }

        public MicroPhase AluOp()
        {
            var source = _state.GetRegister(_sourceRegisterIdx);
            var destination = _state.GetRegister(_destinationRegisterIdx);
            _state.SetZeroFlag(destination == source);
            _state.SetCarryFlag(destination >= source); // Similar to SUB (no borrow), but without actual subtraction
            return MicroPhase.Done;
        }

        private readonly byte _sourceRegisterIdx;
        private readonly byte _destinationRegisterIdx;
        private readonly State _state;
    }
}
