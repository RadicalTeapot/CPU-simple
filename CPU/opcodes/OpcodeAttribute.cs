using CPU.components;

using OpcodeConstrutor = System.Func<byte, CPU.components.State, CPU.components.Memory, CPU.components.Stack, CPU.opcodes.IOpcode>;

namespace CPU.opcodes
{
    /// <summary>
    /// Defines the number of register arguments an opcode has.
    /// </summary>
    /// <remarks>DEPRECATED</remarks>
    internal enum RegisterArgsCount
    {
        Zero,
        One,
        Two,
    }

    /// <summary>
    /// Defines the type of operand an opcode uses.
    /// </summary>
    /// <remarks>DEPRECATED</remarks>
    internal enum OperandType
    {
        None,
        Address,
        Immediate,
        RegAndImmediate,
    }

    /// <summary>
    /// Attribute to declare opcode metadata for auto-discovery and registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class OpcodeAttribute(
        OpcodeBaseCode baseCode,
        OpcodeGroupBaseCode groupCode) : Attribute
    {
        public OpcodeBaseCode BaseCode { get; } = baseCode;
        public OpcodeGroupBaseCode GroupCode { get; } = groupCode;
    }

    /// <summary>
    /// Cached metadata for an opcode, built from <see cref="OpcodeAttribute"/> at startup.
    /// </summary>
    internal record class OpcodeMetadata(
        OpcodeConstrutor Constructor,
        OpcodeBaseCode BaseCode,
        OpcodeGroupBaseCode GroupCode)
    { }
}
