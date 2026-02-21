using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.MOV, OpcodeGroupBaseCode.Move)]
    internal class MOV : IOpcode
    {
        public MOV(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _sourceRegisterIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            _destinationRegisterIdx = OpcodeHelpers.GetHighRegisterIdx(instructionByte);
        }

        public MicroPhase Tick(int phaseCount)
        {
            var value = _state.GetRegister(_sourceRegisterIdx);
            _state.SetRegister(_destinationRegisterIdx, value);
            return MicroPhase.Done;
        }

        private readonly byte _sourceRegisterIdx;
        private readonly byte _destinationRegisterIdx;
        private readonly State _state;
    }
}