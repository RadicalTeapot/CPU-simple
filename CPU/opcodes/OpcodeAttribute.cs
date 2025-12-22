using System.Reflection;

namespace CPU.opcodes
{
    /// <summary>
    /// Defines the number of register arguments an opcode has.
    /// </summary>
    internal enum RegisterArgsCount
    {
        Zero,
        One,
        Two,
    }

    /// <summary>
    /// Defines the type of operand an opcode uses.
    /// </summary>
    internal enum OperandType
    {
        None,
        Address,
        Immediate,
    }

    /// <summary>
    /// Attribute to declare opcode metadata for auto-discovery and registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class OpcodeAttribute(
        OpcodeBaseCode baseCode,
        OpcodeGroupBaseCode groupCode,
        RegisterArgsCount registerArgsCount,
        OperandType operandType) : Attribute
    {
        public OpcodeBaseCode BaseCode { get; } = baseCode;
        public OpcodeGroupBaseCode GroupCode { get; } = groupCode;
        public RegisterArgsCount RegisterArgsCount { get; } = registerArgsCount;
        public OperandType OperandType { get; } = operandType;
    }

    /// <summary>
    /// Cached metadata for an opcode, built from <see cref="OpcodeAttribute"/> at startup.
    /// </summary>
    internal readonly struct OpcodeMetadata(
        ConstructorInfo constructor,
        OpcodeBaseCode baseCode,
        OpcodeGroupBaseCode groupCode,
        RegisterArgsCount registerArgsCount,
        OperandType operandType)
    {
        public ConstructorInfo OpcodeConstructor { get; } = constructor;
        public OpcodeBaseCode BaseCode { get; } = baseCode;
        public OpcodeGroupBaseCode GroupCode { get; } = groupCode;
        public RegisterArgsCount RegisterArgsCount { get; } = registerArgsCount;
        public OperandType OperandType { get; } = operandType;
    }

    /// <summary>
    /// Result of the decode phase: contains the opcode, its metadata, and parsed arguments.
    /// </summary>
    internal readonly struct DecodedInstruction(
        OpcodeMetadata metadata,
        OpcodeArgs args,
        byte rawInstruction)
    {
        public OpcodeMetadata Metadata { get; } = metadata;
        public OpcodeArgs Args { get; } = args;
        public byte RawInstruction { get; } = rawInstruction;
        public ConstructorInfo OpcodeConstructor => Metadata.OpcodeConstructor;
    }
}
