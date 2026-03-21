using System.Diagnostics;
using Controller.Exceptions;
using CPU.components;

namespace Controller.Storage
{
    internal class ControllerRegisters(ButtonsState state) : IMmioDevice
    {
        public byte ReadRegister(byte offset)
        {
            return offset switch
            {
                StatusOffset => ReadStatus(),
                _ => 0,
            };
        }

        public void WriteRegister(byte offset, byte value)
        {
            // Controller is read-only, so we ignore writes.
        }

        private byte ReadStatus()
        {
            Debug.Assert(state.ButtonCount <= 4, "Button count exceeds maximum supported by status register.");
            var currentState = state.ReadAndReset();
            var status = 0;
            if (currentState.Up) status |= 1 << 0;
            if (currentState.Down) status |= 1 << 1;
            if (currentState.Left) status |= 1 << 2;
            if (currentState.Right) status |= 1 << 3;
            for (var i = 0; i < state.ButtonCount; i++)
            {
                if (currentState.Buttons[i])
                    status |= 1 << (4 + i);
            }
            return (byte)status;
        }

        private const int StatusOffset = 0;
    }
}
