using System.Reflection;
using System.Text;

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
        RegAndImmediate,
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
    internal class DecodedInstruction(
        OpcodeMetadata metadata,
        OpcodeArgs args,
        byte rawInstruction)
    {
        public OpcodeMetadata Metadata { get; } = metadata;
        public OpcodeArgs Args { get; } = args;
        public byte RawInstruction { get; } = rawInstruction;
        public ConstructorInfo OpcodeConstructor => Metadata.OpcodeConstructor;

        public string[] AsStringArray()
        {
            var parts = new List<string> { Metadata.BaseCode.ToString() };

            if (Metadata.RegisterArgsCount == RegisterArgsCount.Two)
            {
                parts.Add($"R{Args.LowRegisterIdx}");
                parts.Add($"R{Args.HighRegisterIdx}");
            }
            else if (Metadata.RegisterArgsCount == RegisterArgsCount.One)
            {
                parts.Add($"R{Args.LowRegisterIdx}");
            }

            if (Metadata.OperandType == OperandType.Address)
            {
                parts.Add($"[{Args.AddressValue:X2}]");
            }
            else if (Metadata.OperandType == OperandType.Immediate)
            {
                parts.Add($"#0x{Args.ImmediateValue:X2}");
            }
            else if (Metadata.OperandType == OperandType.RegAndImmediate)
            {
                parts.Add($"[R{Args.IndirectRegisterIdx}+#0x{Args.ImmediateValue:X2}]");
            }

            return parts.ToArray();
        }
    }
}
