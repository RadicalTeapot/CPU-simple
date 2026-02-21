using CPU.components;
using CPU.microcode;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.BTI, OpcodeGroupBaseCode.SingleRegisterLogicOne)]
    internal class BTI : BaseOpcode
    {
        public BTI(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _memory = memory;
            _registerIdx = OpcodeHelpers.GetDestinationRegisterIdx(instructionByte);
            SetPhases(MicroPhase.MemoryRead, ReadImmediateValue, AluOp);
        }

        private MicroPhase ReadImmediateValue()
        {
            _immediateValue = _memory.ReadByte(_state.GetPC());
            _state.IncrementPC();
            return MicroPhase.AluOp;
        }

        private MicroPhase AluOp()
        {
            var test = (byte)(_state.GetRegister(_registerIdx) & _immediateValue);
            _state.SetZeroFlag(test != 0); // Test if any bit matches and value is not zero
            return MicroPhase.Done;
        }

        private byte _immediateValue;
        private readonly byte _registerIdx;
        private readonly State _state;
        private readonly Memory _memory;
    }
}
