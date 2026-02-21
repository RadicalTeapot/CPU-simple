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
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            _indirectRegisterIdx = OpcodeHelpers.GetHighRegisterIdx(instructionByte);
            SetPhases(ReadImmediate, AluOp, GetMemoryValue);
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

        private MicroPhase GetMemoryValue()
        {
            var value = _memory.ReadByte(_effectiveAddress);
            _state.SetRegister(_registerIdx, value);
            return MicroPhase.MemoryRead;
        }

        private byte _immediateValue;
        private byte _effectiveAddress;
        private readonly byte _registerIdx;
        private readonly byte _indirectRegisterIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
