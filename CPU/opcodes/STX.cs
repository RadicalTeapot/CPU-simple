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
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            _indirectRegisterIdx = OpcodeHelpers.GetHighRegisterIdx(instructionByte);
            SetPhases(ReadImmediate, AluOp, Write);
        }

        private MicroPhase ReadImmediate()
        {
            _immediateValue = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
        }

        private MicroPhase AluOp()
        {
            _effectiveAddress = (byte)(_state.GetRegister(_indirectRegisterIdx) + _immediateValue);
            return MicroPhase.AluOp;
        }

        private MicroPhase Write()
        {
            var value = _state.GetRegister(_registerIdx);
            _memory.WriteByte(_effectiveAddress, value);
            return MicroPhase.MemoryWrite;
        }

        private byte _immediateValue;
        private byte _effectiveAddress;
        private readonly byte _registerIdx;
        private readonly byte _indirectRegisterIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
