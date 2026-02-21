using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.PSH, OpcodeGroupBaseCode.StoreAndIndirect)]
    internal class PSH : BaseOpcode
    {
        public PSH(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _stack = stack;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.MemoryWrite, Push);
        }

        private MicroPhase Push()
        {
            var value = _state.GetRegister(_registerIdx);
            _stack.PushByte(value);
            return MicroPhase.Done;
        }

        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Stack _stack;
    }
}
