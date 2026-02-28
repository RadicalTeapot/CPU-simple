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

        public OpcodeBaseCode GetOpcodeBaseCodeFromInstruction(byte instruction)
        {
            var groupCode = GetGroupCode(instruction);
            var mask = OpcodeGroupMasks.Mask[groupCode];
            return (OpcodeBaseCode)(instruction & mask);
        }

        public IOpcode CreateOpcode(byte instructionByte, State state, IBus bus, Stack stack)
        {
            var baseCode = GetOpcodeBaseCodeFromInstruction(instructionByte);
            if (!_opcodeMetadataCache.TryGetValue(baseCode, out var metadata))
                throw new InvalidOperationException($"No opcode registered for base code {baseCode} (instruction was {instructionByte:X2})");

            return metadata.Constructor(instructionByte, state, bus, stack);
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
                var attribute = type.GetCustomAttribute<OpcodeAttribute>();
                Debug.Assert(attribute != null, $"Opcode class {type.Name} must have an OpcodeAttribute.");

                // Standard constructor signature: (byte, State, IBus, Stack)
                var constructor = type.GetConstructor([typeof(byte), typeof(State), typeof(IBus), typeof(Stack)])
                    ?? throw new InvalidOperationException($"Opcode {type.Name} must have a constructor with signature (byte, State, IBus, Stack)");
                Debug.Assert(typeof(IOpcode).IsAssignableFrom(constructor.DeclaringType),
                    "Decoded opcode constructor must belong to a type implementing IOpcode.");

                var metadata = new OpcodeMetadata(
                    (instructionByte, state, memory, stack) => (IOpcode)constructor.Invoke([instructionByte, state, memory, stack]),
                    attribute.BaseCode,
                    attribute.GroupCode);

                opcodeMetadataCache[attribute.BaseCode] = metadata;
            }
            return opcodeMetadataCache;
        }

        private const byte GROUP_MASK = 0xF0;

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
