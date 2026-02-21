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
            _sourceRegisterIdx = OpcodeHelpers.GetSourceRegisterIdx(instructionByte);
            _destinationRegisterIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
        }

        public MicroPhase GetStartPhaseType() => MicroPhase.Done;

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