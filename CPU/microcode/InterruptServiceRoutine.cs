using CPU.components;
using CPU.opcodes;

namespace CPU.microcode
{
    /// <summary>
    /// Internal-only opcode created by TickHandler when servicing an interrupt.
    /// Not registered with OpcodeFactory (no [Opcode] attribute).
    /// Pushes status byte and PC to stack, sets I flag, and jumps to IRQ vector.
    /// </summary>
    /// <remarks>
    /// Status byte encoding: (I &lt;&lt; 2) | (C &lt;&lt; 1) | Z
    /// </remarks>
    internal class InterruptServiceRoutine : BaseOpcode
    {
        public InterruptServiceRoutine(State state, Stack stack, int irqVectorAddress)
        {
            _state = state;
            _stack = stack;
            _irqVectorAddress = irqVectorAddress;
#if x16
            SetPhases(MicroPhase.MemoryWrite, PushStatus, PushPCHigh, PushPCLow);
#else
            SetPhases(MicroPhase.MemoryWrite, PushStatus, PushPC);
#endif
        }

        public override string ToString() => "ISR";

        private MicroPhase PushStatus()
        {
            _stack.PushByte(PackFlags());
            return MicroPhase.MemoryWrite;
        }

#if x16
        private MicroPhase PushPCHigh()
        {
            _stack.PushByte((byte)(_state.GetPC() >> 8));
            return MicroPhase.MemoryWrite;
        }

        private MicroPhase PushPCLow()
        {
            _stack.PushByte((byte)(_state.GetPC() & 0xFF));
            _state.SetInterruptDisableFlag(true);
            _state.SetPC((ushort)_irqVectorAddress);
            return MicroPhase.Done;
        }
#else
        private MicroPhase PushPC()
        {
            _stack.PushByte(_state.GetPC());
            _state.SetInterruptDisableFlag(true);
            _state.SetPC((byte)_irqVectorAddress);
            return MicroPhase.Done;
        }
#endif

        private byte PackFlags()
        {
            return (byte)(
                (_state.GetInterruptDisableFlagAsInt() << 2) |
                (_state.GetCarryFlagAsInt() << 1) |
                _state.GetZeroFlagAsInt());
        }

        private readonly State _state;
        private readonly Stack _stack;
        private readonly int _irqVectorAddress;
    }
}
