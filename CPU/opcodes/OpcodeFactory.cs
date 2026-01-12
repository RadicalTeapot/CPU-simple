using CPU.components;
using System.Diagnostics;
using System.Reflection;

namespace CPU.opcodes
{
    /// <summary>
    /// Factory class responsible for creating and managing opcodes.
    /// </summary>
    /// <remarks>This plays the role of the "Control unit" in a CPU architecture.</remarks>
    internal class OpcodeFactory
    {
        public OpcodeFactory()
        {
            _opcodeMetadataCache = DiscoverAndRegisterOpcodes();
        }

        public byte GetInstructionSize(byte instruction)
        {
            var metadata = GetOpcodeMetadata(instruction);
            return metadata.OperandType switch
            {
                OperandType.None => 1,
                OperandType.Address => 1 + AddressSize,
                OperandType.Immediate => 2,
                OperandType.RegAndImmediate => 2,
                _ => throw new InvalidOperationException($"Unknown operand type: {metadata.OperandType}"),
            };
        }

        /// <summary>
        /// Decodes an instruction: resolves the opcode, parses registers and operands.
        /// </summary>
        /// <param name="instructionBytes">The raw instruction bytes (already fetched)</param>
        /// <returns>Decoded instruction with opcode reference and parsed arguments</returns>
        public DecodedInstruction Decode(byte[] instructionBytes)
        {
            Debug.Assert(instructionBytes.Length > 0, "Instruction bytes cannot be empty.");
            var metadata = GetOpcodeMetadata(instructionBytes[0]);
            var args = ParseArguments(instructionBytes, metadata);
            return new DecodedInstruction(metadata, args, instructionBytes[0]);
        }

        /// <summary>
        /// Gets opcode metadata from an instruction byte.
        /// </summary>
        private OpcodeMetadata GetOpcodeMetadata(byte instruction)
        {
            var groupCode = GetGroupCode(instruction);
            var mask = OpcodeGroupMasks.Mask[groupCode];
            var opcodeBaseCode = (OpcodeBaseCode)(instruction & mask);

            Debug.Assert(
                _opcodeMetadataCache.ContainsKey(opcodeBaseCode),
                $"Unregistered opcode base code: {opcodeBaseCode} (instruction was {instruction:X2})");

            return _opcodeMetadataCache[opcodeBaseCode];
        }

        /// <summary>
        /// Parses arguments from instruction bytes.
        /// </summary>
        private static OpcodeArgs ParseArguments(byte[] instructionBytes, OpcodeMetadata metadata)
        {
            var args = new OpcodeArgs();

            // Parse register indices from instruction
            var instruction = instructionBytes[0];
            switch (metadata.RegisterArgsCount)
            {
                case RegisterArgsCount.Zero:
                    break;
                case RegisterArgsCount.One:
                    args.LowRegisterIdx = (byte)(instruction & REGISTER_MASK);
                    break;
                case RegisterArgsCount.Two:
                    args.HighRegisterIdx = (byte)((instruction >> 2) & REGISTER_MASK);
                    args.LowRegisterIdx = (byte)(instruction & REGISTER_MASK);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown register args count: {metadata.RegisterArgsCount}");
            }

            // Fetch operand from instruction bytes
            switch (metadata.OperandType)
            {
                case OperandType.None:
                    break;
                case OperandType.Address:
                    args.AddressValue = GetAddressFromInstructionBytes(instructionBytes);
                    break;
                case OperandType.Immediate:
                    args.ImmediateValue = instructionBytes[1];
                    break;
                case OperandType.RegAndImmediate:
                    args.IndirectRegisterIdx = (byte)(instructionBytes[1] & REGISTER_MASK);
                    args.ImmediateValue = (byte)(instructionBytes[1] >> 2);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown operand type: {metadata.OperandType}");
            }

            return args;
        }

        /// <summary>
        /// Extracts the opcode group code from the instruction's upper nibble.
        /// </summary>
        private static OpcodeGroupBaseCode GetGroupCode(byte instruction)
        {
            var groupByte = (byte)(instruction & GROUP_MASK);

            Debug.Assert(
                Enum.IsDefined(typeof(OpcodeGroupBaseCode), groupByte),
                $"Unknown opcode group byte: {groupByte:X2} (instruction was {instruction:X2})");

            return (OpcodeGroupBaseCode)groupByte;
        }

        /// <summary>
        /// Discovers all opcode classes with <see cref="OpcodeAttribute"/> and registers them.
        /// </summary>
        private static Dictionary<OpcodeBaseCode, OpcodeMetadata> DiscoverAndRegisterOpcodes()
        {
            var opcodeTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetCustomAttribute<OpcodeAttribute>() != null && typeof(IOpcode).IsAssignableFrom(t));

            var opcodeMetadataCache = new Dictionary<OpcodeBaseCode, OpcodeMetadata>();
            foreach (var type in opcodeTypes)
            {
                var attribute = type.GetCustomAttribute<OpcodeAttribute>()!;

                // Standard constructor signature: (State, Memory, Stack, OpcodeArgs)
                var constructor = type.GetConstructor([typeof(State), typeof(Memory), typeof(Stack), typeof(OpcodeArgs)]) 
                    ?? throw new InvalidOperationException($"Opcode {type.Name} must have a constructor with signature (State, Memory, Stack, OpcodeArgs)");

                var metadata = new OpcodeMetadata(
                    constructor,
                    attribute.BaseCode,
                    attribute.GroupCode,
                    attribute.RegisterArgsCount,
                    attribute.OperandType);

                opcodeMetadataCache[attribute.BaseCode] = metadata;
            }
            return opcodeMetadataCache;
        }

        private const byte GROUP_MASK = 0xF0;
        private const byte REGISTER_MASK = 0x03;

        private readonly Dictionary<OpcodeBaseCode, OpcodeMetadata> _opcodeMetadataCache;

#if x16
        public const int AddressSize = 2;
        private static ushort GetAddressFromInstructionBytes(byte[] instructionBytes) 
            => (ushort)((instructionBytes[2] << 8) | instructionBytes[1]);
#else
        public const int AddressSize = 1;
        private static byte GetAddressFromInstructionBytes(byte[] instructionBytes) 
            => instructionBytes[1];
#endif
    }
}
