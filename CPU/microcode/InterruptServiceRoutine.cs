using CPU.components;
using CPU.opcodes;

namespace CPU.microcode
{
    /// <summary>
    /// Internal-only opcode created by TickHandler when servicing an interrupt.
    /// Not registered with OpcodeFactory (no [Opcode] attribute).
    /// Pushes status byte and PC to stack, sets I flag, then reads the handler address
    /// from the IRQ vector table and jumps to it.
    /// </summary>
    /// <remarks>
    /// Status byte encoding: (I &lt;&lt; 2) | (C &lt;&lt; 1) | Z
    /// </remarks>
    internal class InterruptServiceRoutine : BaseOpcode
    {
        public InterruptServiceRoutine(State state, Stack stack, IBus bus, int irqVectorTableAddress)
        {
            _state = state;
            _stack = stack;
            _bus = bus;
            _irqVectorTableAddress = irqVectorTableAddress;
#if x16
            SetPhases(MicroPhase.MemoryWrite, PushStatus, PushPCHigh, PushPCLow, ReadVectorLow, ReadVectorHigh);
#else
            SetPhases(MicroPhase.MemoryWrite, PushStatus, PushPC, ReadVector);
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
            return MicroPhase.MemoryRead;
        }

        private MicroPhase ReadVectorLow()
        {
            _vectorLow = _bus.ReadByte((ushort)_irqVectorTableAddress);
            return MicroPhase.MemoryRead;
        }

        private MicroPhase ReadVectorHigh()
        {
            var vectorHigh = _bus.ReadByte((ushort)(_irqVectorTableAddress + 1));
            _state.SetInterruptDisableFlag(true);
            _state.SetPC((ushort)((vectorHigh << 8) | _vectorLow));
            return MicroPhase.Done;
        }

        private byte _vectorLow;
#else
        private MicroPhase PushPC()
        {
            _stack.PushByte(_state.GetPC());
            return MicroPhase.MemoryRead;
        }

        private MicroPhase ReadVector()
        {
            var handlerAddress = _bus.ReadByte((byte)_irqVectorTableAddress);
            _state.SetInterruptDisableFlag(true);
            _state.SetPC(handlerAddress);
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
        private readonly IBus _bus;
        private readonly int _irqVectorTableAddress;
    }
}
