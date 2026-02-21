using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.POP, OpcodeGroupBaseCode.Load)]
    internal class POP : BaseOpcode
    {
        public POP(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _stack = stack;
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            SetPhases(Pop);
        }

        private MicroPhase Pop()
        {
            var value = _stack.PopByte();
            _state.SetRegister(_registerIdx, value);
            return MicroPhase.MemoryRead;
        }

        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Stack _stack;
    }
}
