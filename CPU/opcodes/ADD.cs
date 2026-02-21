using CPU.components;
using CPU.microcode;
using System;

namespace CPU.opcodes
{
    [Opcode(OpcodeBaseCode.ADD, OpcodeGroupBaseCode.Add)]
    internal class ADD : BaseOpcode
    {
        public ADD(byte instructionByte, State state, Memory memory, Stack stack)
        {
            _state = state;
            _sourceRegisterIdx = OpcodeHelpers.GetLowRegisterIdx(instructionByte);
            _destinationRegisterIdx = OpcodeHelpers.GetHighRegisterIdx(instructionByte);
            SetPhases(AluOp);
        }

        public MicroPhase AluOp()
        {
            var firstValue = _state.GetRegister(_sourceRegisterIdx);
            var secondValue = _state.GetRegister(_destinationRegisterIdx);
            var result = firstValue + secondValue + _state.GetCarryFlagAsInt();
            _state.SetRegister(_destinationRegisterIdx, (byte)result); // Wrap around on overflow
            _state.SetCarryFlag(result > 0xFF);
            _state.SetZeroFlag(result == 0);
            return MicroPhase.AluOp;

        }

        private readonly byte _sourceRegisterIdx;
        private readonly byte _destinationRegisterIdx;
        private readonly State _state;
    }
}