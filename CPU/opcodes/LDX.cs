using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.LDX, OpcodeGroupBaseCode.StoreAndIndirect)]
    internal class LDX : BaseOpcode
    {
        public LDX(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.MemoryRead, ReadOffsetAndImmediate, AluOp, GetMemoryValue);
        }

        private MicroPhase ReadOffsetAndImmediate()
        {
            var value = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            _indirectRegisterIdx = (byte)(value & 0b11);
            _immediateValue = (byte)(value >> 2);
            return MicroPhase.AluOp;
        }

        private MicroPhase AluOp()
        {
            var offset = _state.GetRegister(_indirectRegisterIdx);
            _effectiveAddress = (byte)(offset + _immediateValue);
            return MicroPhase.MemoryRead;
        }

        private MicroPhase GetMemoryValue()
        {
            var value = _memory.ReadByte(_effectiveAddress);
            _state.SetRegister(_registerIdx, value);
            return MicroPhase.Done;
        }

        private byte _immediateValue;
        private byte _effectiveAddress;
        private byte _indirectRegisterIdx;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
