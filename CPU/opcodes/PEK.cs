using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.PEK, OpcodeGroupBaseCode.Load)]
    internal class PEK : BaseOpcode
    {
        public PEK(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _stack = stack;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.MemoryRead, Peek);
        }

        private MicroPhase Peek()
        {
            var value = _stack.PeekByte();
            _state.SetRegister(_registerIdx, value);
            return MicroPhase.Done;
        }

        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Stack _stack;
    }
}
