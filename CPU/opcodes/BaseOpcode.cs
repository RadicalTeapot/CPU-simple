using CPU.components;

namespace CPU.opcodes
{
    internal struct OpcodeArgs()
    {
        /// <summary>
        /// Bits 2-3 register index, typically source register, if applicable.
        /// </summary>
        public byte HighRegisterIdx = 0;
        /// <summary>
        /// Bits 0-1 register index, typically destination register, if applicable.
        /// </summary>
        public byte LowRegisterIdx = 0;
        public byte ImmediateValue = 0;
#if x16
        public ushort AddressValue = 0;
#else
        public byte AddressValue = 0;
#endif
    }

    /// <summary>
    /// Base class for opcodes that follow the standard execution pattern.
    /// </summary>
    /// <remarks>
    /// Opcodes should be decorated with <see cref="OpcodeAttribute"/> for auto-discovery.
    /// The standard constructor signature is (State, Memory, Stack) for dependency injection.
    /// </remarks>
    internal abstract class BaseOpcode(State cpuState, Memory memory, Stack stack) : IOpcode
    {
        protected readonly State CpuState = cpuState;
        protected readonly Memory Memory = memory;
        protected readonly Stack Stack = stack;

        /// <summary>
        /// Executes the opcode with the given arguments.
        /// </summary>
        /// <param name="args">Parsed opcode arguments</param>
        public abstract void Execute(OpcodeArgs args);
    }
}