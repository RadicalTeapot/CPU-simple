using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.STX, OpcodeGroupBaseCode.StoreAndIndirect)]
    internal class STX : BaseOpcode
    {
        public STX(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.MemoryRead, ReadRegisterAndImmediate, AluOp, Write);
        }

        private MicroPhase ReadRegisterAndImmediate()
        {
            var value = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            _indirectRegisterIdx = (byte)(value & 0b11);
            _immediateValue = (byte)(value >> 2);
            return MicroPhase.AluOp;
        }

        private MicroPhase AluOp()
        {
            _effectiveAddress = (byte)(_state.GetRegister(_indirectRegisterIdx) + _immediateValue);
            return MicroPhase.MemoryWrite;
        }

        private MicroPhase Write()
        {
            var value = _state.GetRegister(_registerIdx);
            _memory.WriteByte(_effectiveAddress, value);
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
