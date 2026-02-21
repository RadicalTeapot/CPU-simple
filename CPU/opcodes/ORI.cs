using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ORI, OpcodeGroupBaseCode.SingleRegisterLogicOne)]
    internal class ORI : BaseOpcode
    {
        public ORI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            SetPhases(ReadImmediateValue, AluOp);
        }

        private MicroPhase ReadImmediateValue()
        {
            _immediateValue = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.MemoryRead;
        }

        private MicroPhase AluOp()
        {
            var registerValue = _state.GetRegister(_registerIdx);
            var value = (byte)(registerValue | _immediateValue);
            _state.SetRegister(_registerIdx, value);
            _state.SetZeroFlag(value == 0);
            return MicroPhase.AluOp;
        }

        private MicroPhase Done() => MicroPhase.Done;

        private byte _immediateValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
