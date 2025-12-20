namespace CPU.opcodes
{
    internal interface IOpcodeGroup
    {
        void RegisterGroup(Dictionary<OpcodeGroupBaseCode, IOpcodeGroup> opcodeGroupRegistry);
        OpcodeBaseCode ExtractOpcodeBaseCodeFromInstruction(byte instruction);
    }

    internal enum OpcodeGroupBaseCode : byte
    {
        SYSTEM_AND_JUMP = 0x00,
        LOAD = 0x10,
        STORE = 0x20,
        MOVE = 0x30,
        SINGLE_REGISTER_ALU = 0x40,
        TWO_REGISTERS_COMPARE = 0x90,
    }

    internal abstract class BaseOpcodeGroup(OpcodeGroupBaseCode code, byte baseOpcodeMask) : IOpcodeGroup
    {
        public void RegisterGroup(Dictionary<OpcodeGroupBaseCode, IOpcodeGroup> opcodeGroupRegistry) 
            => opcodeGroupRegistry[code] = this;

        public OpcodeBaseCode ExtractOpcodeBaseCodeFromInstruction(byte instruction)
        {
            var baseOpcodeByte = (byte)(instruction & baseOpcodeMask);
            if (!Enum.IsDefined(typeof(OpcodeBaseCode), baseOpcodeByte))
            {
                throw new KeyNotFoundException($"Unknown opcode byte: {baseOpcodeByte:X2}");
            }
            return (OpcodeBaseCode)baseOpcodeByte;
        }
    }

    internal class SystemAndJumpOpcodeGroup() : BaseOpcodeGroup(OpcodeGroupBaseCode.SYSTEM_AND_JUMP, 0xFF) { }
    internal class LoadOpcodeGroup() : BaseOpcodeGroup(OpcodeGroupBaseCode.LOAD, 0xFC) { }
    internal class StoreOpcodeGroup() : BaseOpcodeGroup(OpcodeGroupBaseCode.STORE, 0xFC) { }
    internal class MoveOpcodeGroup() : BaseOpcodeGroup(OpcodeGroupBaseCode.MOVE, 0xF0) { }
    internal class SingleRegisterALUOpcodeGroup() : BaseOpcodeGroup(OpcodeGroupBaseCode.SINGLE_REGISTER_ALU, 0xFC) { }
    internal class TwoRegistersCompareOpcodeGroup() : BaseOpcodeGroup(OpcodeGroupBaseCode.TWO_REGISTERS_COMPARE, 0xF0) { }

}
