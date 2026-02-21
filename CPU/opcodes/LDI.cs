using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDI, OpcodeGroupBaseCode.Load)]
    internal class LDI : BaseOpcode
    {
        public LDI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.MemoryRead, ReadImmediateValue);
        }

        private MicroPhase ReadImmediateValue()
        {
            var value = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            _state.SetRegister(_registerIdx, value);
            return MicroPhase.Done;
        }

        private readonly byte _registerIdx;
        private readonly Memory _memory;
        private readonly State _state;
    }
}